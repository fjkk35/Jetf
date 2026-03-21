using Dapper;
using Service.Models;
using Service.Services.PdtScanCargoArrivalTime.Domain;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.PdtScanCargoArrivalTime
{
    public class PdtScanCargoArrivalTimeService : _BaseService
    {
        public List<SelectListModel> GetDataTypeList()
        {
            const string sql = @"
SELECT DataType AS [Value], DataType AS [Text]
FROM [jetf].[dbo].[PdtDataType]";

            return conn.Query<SelectListModel>(sql).ToList();
        }

        public List<SelectListModel> GetTransList()
        {
            const string sql = @"
SELECT TransNo AS [Value], TransName AS [Text]
FROM [jetf].[dbo].[PdtTrans]";

            return conn.Query<SelectListModel>(sql).ToList();
        }

        public List<PdtScanCargoArrivalTimeGroupModel> Search(PdtScanCargoArrivalTimeRequest request)
        {
            var rows = conn.Query<PdtScanCargoArrivalTimeQueryRow>(
                "[dbo].[USP_GetPdtScanCargoArrivalTime]",
                new
                {
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TransNo = request.TransNo,
                    DataType = request.DataType
                },
                commandType: System.Data.CommandType.StoredProcedure,
                commandTimeout:120).ToList();

            var transNameMap = GetTransNameMap();

            var grouped = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.TransNo))
                .GroupBy(x => x.TransNo)
                .Select(g =>
                {
                    var transNo = g.Key;
                    var transName = transNameMap.ContainsKey(g.Key) ? transNameMap[g.Key] : transNo;

                    var lastArrivalRow = g
                        .Where(x => x.ArrivalTime.HasValue)
                        .OrderByDescending(x => x.ArrivalTime)
                        .FirstOrDefault();

                    return new PdtScanCargoArrivalTimeGroupModel
                    {
                        TransNo = transNo,
                        TransName = transName,
                        TotalCount = g.Count(),
                        ArrivedCount = g.Count(x => x.ArrivalTime.HasValue),
                        LastArrivalTime = lastArrivalRow?.ArrivalTime,
                        LastUpdateArrivalTime = lastArrivalRow?.UpdateArrivalTime,
                        LastUpdateArrivalTimeOpe = lastArrivalRow?.UpdateArrivalTimeOpe,
                        Ids = g.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
                        Details = ToDetails(transName, g.ToList())
                    };
                })
                .OrderBy(x => x.TransNo)
                .ToList();

            return grouped;
        }

        public ResopnseModel UpdateArrivalTime(DateTime arrivalTime, string transName, List<string> ids)
        {
            var result = new ResopnseModel();

            if (ids == null || ids.Count == 0)
            {
                result.status = Status.error;
                result.msg = "更新失敗：未提供要更新的資料";
                return result;
            }

            try
            {
                var validateResult = ValidateArrivalTime(arrivalTime, transName, ids);
                if (validateResult != null)
                {
                    return validateResult;
                }

                var updateArrivalTime = DateTime.Now;
                var updateArrivalTimeOpe = GetUserId();

                const string updateSql = @"
UPDATE [jetf].[dbo].[PdtScanCargoUpload]
SET
    ArrivalTime = @ArrivalTime,
    UpdateArrivalTime = @UpdateArrivalTime,
    UpdateArrivalTimeOpe = @UpdateArrivalTimeOpe
FROM [jetf].[dbo].[PdtScanCargoUpload] p
INNER JOIN @Ids i ON p.Id = i.Value";

                var idsTable = CreateStringListTable(ids);

                var rowsAffected = conn.Execute(updateSql, new
                {
                    ArrivalTime = arrivalTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdateArrivalTime = updateArrivalTime,
                    UpdateArrivalTimeOpe = updateArrivalTimeOpe,
                    Ids = idsTable.AsTableValuedParameter("StringListType")
                });

                result.status = Status.success;
                result.msg = $"更新成功，共更新 {rowsAffected} 筆資料";
                result.ReturnObject = new
                {
                    ArrivedCount = rowsAffected,
                    LastArrivalTime = arrivalTime,
                    LastUpdateArrivalTime = updateArrivalTime,
                    LastUpdateArrivalTimeOpe = updateArrivalTimeOpe
                };
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = ex.Message;
            }

            return result;
        }

        private ResopnseModel ValidateArrivalTime(DateTime arrivalTime, string transName, List<string> ids)
        {
            const string getMinUploadTimeSql = @"
SELECT MIN(p.UploadTime)
FROM [jetf].[dbo].[PdtScanCargoUpload] p
INNER JOIN @Ids i ON p.Id = i.Value";

            const string getMaxUploadTimeSql = @"
SELECT MAX(p.UploadTime)
FROM [jetf].[dbo].[PdtScanCargoUpload] p
INNER JOIN @Ids i ON p.Id = i.Value";

            var idsTable = CreateStringListTable(ids);

            var minUploadTime = conn.Query<DateTime?>(getMinUploadTimeSql, new
            {
                Ids = idsTable.AsTableValuedParameter("StringListType")
            }).FirstOrDefault();

            if (!minUploadTime.HasValue)
            {
                return new ResopnseModel
                {
                    status = Status.error,
                    msg = "更新失敗：找不到對應的掃讀資料"
                };
            }

            if (arrivalTime < minUploadTime.Value)
            {
                return new ResopnseModel
                {
                    status = Status.error,
                    msg = $"更新失敗：交倉時間不可小於掃讀時間({minUploadTime.Value:yyyy-MM-dd HH:mm:ss})"
                };
            }

            var maxArrivalTime = minUploadTime.Value.AddDays(3);
            if (arrivalTime > maxArrivalTime)
            {
                return new ResopnseModel
                {
                    status = Status.error,
                    msg = $"更新失敗：交倉時間不可大於掃讀時間 + 3 天({maxArrivalTime:yyyy-MM-dd HH:mm:ss})"
                };
            }

            // SPX、FM 交倉時間需限制在最後掃讀時間起算 15 小時內。
            if (NeedValidateTransArrivalTime(transName))
            {
                var maxUploadTime = conn.Query<DateTime?>(getMaxUploadTimeSql, new
                {
                    Ids = idsTable.AsTableValuedParameter("StringListType")
                }).FirstOrDefault();

                if (!maxUploadTime.HasValue)
                {
                    return new ResopnseModel
                    {
                        status = Status.error,
                        msg = "更新失敗：找不到對應的掃讀資料"
                    };
                }

                var maxAllowedArrivalTime = maxUploadTime.Value.AddHours(15);
                // 允許區間為「最後掃讀時間」到「最後掃讀時間 + 15 小時」。
                if (arrivalTime < maxUploadTime.Value || arrivalTime > maxAllowedArrivalTime)
                {
                    return new ResopnseModel
                    {
                        status = Status.error,
                        msg = $"更新失敗：{transName} 的交倉時間需介於最後掃讀時間 {maxUploadTime.Value:yyyy-MM-dd HH:mm:ss} 到 {maxAllowedArrivalTime:yyyy-MM-dd HH:mm:ss}"
                    };
                }
            }

            return null;
        }

        // 指定物流商需套用最後掃讀時間 15 小時內的交倉時間檢核。
        private bool NeedValidateTransArrivalTime(string transName)
        {
            if (string.IsNullOrWhiteSpace(transName))
            {
                return false;
            }

            return transName.IndexOf("SPX", StringComparison.OrdinalIgnoreCase) >= 0
                || transName.IndexOf("FM", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private DataTable CreateStringListTable(List<string> values)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));

            foreach (var value in values)
            {
                table.Rows.Add(value);
            }

            return table;
        }

        private List<PdtScanCargoArrivalTimeDetailModel> ToDetails(string transName, List<PdtScanCargoArrivalTimeQueryRow> rows)
        {
            if (rows == null) return new List<PdtScanCargoArrivalTimeDetailModel>();

            return rows
                .GroupBy(x => new { x.ArrivalTime, x.UpdateArrivalTime, x.UpdateArrivalTimeOpe })
                .Select(g => new PdtScanCargoArrivalTimeDetailModel
                {
                    TransName = transName,
                    ArrivalTime = g.Key.ArrivalTime,
                    ArrivedCount = g.Count(x => x.ArrivalTime.HasValue),
                    UpdateArrivalTime = g.Key.UpdateArrivalTime,
                    UpdateArrivalTimeOpe = g.Key.UpdateArrivalTimeOpe
                })
                .Where(r => r.ArrivedCount > 0)
                .OrderByDescending(x => x.ArrivalTime)
                .ToList();
        }

        private Dictionary<string, string> GetTransNameMap()
        {
            const string sql = @"
SELECT DISTINCT TRANS_NO, TRANS_NAME
FROM [dbo].[customer_master]
WHERE TRAN_TYPE = N'空運'";

            return conn.Query<TransNameDto>(sql)
             .GroupBy(x => x.TRANS_NO)
             .ToDictionary(
                 g => g.Key,
                 g => g.First().TRANS_NAME
             );
        }

        private class TransNameDto
        {
            public string TRANS_NO { get; set; }
            public string TRANS_NAME { get; set; }
        }

    }
}
