using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentOutboundBatchImportRevoke.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentOutboundBatchImportRevoke
{
    public class ShipmentOutboundBatchImportRevokeService : _BaseService
    {
        /// <summary>
        /// 批量取消貨件出庫
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="userName">使用者名稱</param>
        /// <returns></returns>
        public ResponseModel RevokeOutbound(string filePath)
        {
            try
            {
                var revokeList = ReadExcelFile(filePath);

                if (revokeList.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                ValidateAndFetchData(revokeList);

                var failList = revokeList.Where(x => x.Status == "失敗").ToList();

                if (failList.Count > 0)
                {
                    return new ResponseModel(new
                    {
                        count = 0,
                        failCount = failList.Count,
                        data = revokeList,
                        message = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，請修正後重新上傳"
                    });
                }

                var successList = revokeList.Where(x => x.Status == "成功").ToList();

                RevokeOutboundData(successList);

                return new ResponseModel(new
                {
                    count = successList.Count,
                    failCount = 0,
                    data = revokeList,
                    message = $"成功取消 {successList.Count} 筆出庫資料"
                });
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 讀取 Excel 檔案
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <returns></returns>
        private List<ShipmentOutboundRevokeModel> ReadExcelFile(string filePath)
        {
            var revokeList = new List<ShipmentOutboundRevokeModel>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var trackingNo = row.GetCellData(0);

                    if (string.IsNullOrWhiteSpace(trackingNo))
                        continue;

                    var model = new ShipmentOutboundRevokeModel
                    {
                        TrackingNo = trackingNo,
                        Status = "待處理"
                    };

                    revokeList.Add(model);
                }
            }

            return revokeList;
        }

        /// <summary>
        /// 驗證並取得資料
        /// </summary>
        /// <param name="revokeList">取消出庫資料列表</param>
        private void ValidateAndFetchData(List<ShipmentOutboundRevokeModel> revokeList)
        {
            if (revokeList.Count == 0)
                return;

            var trackingNos = revokeList.Select(x => x.TrackingNo).Distinct().ToList();

            var threeDaysAgo = DateTime.Now.Date.AddDays(-3);

            var sql = @"
                SELECT 
                    Id,
                    TrackingNo,
                    OutboundDate,
                    OutboundTrackingNo
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE TrackingNo IN @TrackingNos 
                AND OutboundDate IS NOT NULL";

            conn.Open();
            var existingData = conn.Query<dynamic>(sql, new { TrackingNos = trackingNos }).ToList();
            conn.Close();

            var dataDict = existingData.ToDictionary(
                x => (string)x.TrackingNo,
                x => x
            );

            foreach (var revoke in revokeList)
            {
                if (string.IsNullOrWhiteSpace(revoke.TrackingNo))
                {
                    revoke.Status = "失敗";
                    revoke.FailReason = "單號為空";
                    continue;
                }

                if (!dataDict.ContainsKey(revoke.TrackingNo))
                {
                    revoke.Status = "失敗";
                    revoke.FailReason = "查無此單號或此單號未出庫";
                    continue;
                }

                var data = dataDict[revoke.TrackingNo];
                revoke.ShipmentInboundId = (int)data.Id;
                revoke.OutboundDate = (DateTime?)data.OutboundDate;
                revoke.OutboundTrackingNo = (string)data.OutboundTrackingNo;

                if (revoke.OutboundDate.HasValue && revoke.OutboundDate.Value.Date < threeDaysAgo)
                {
                    revoke.Status = "失敗";
                    revoke.FailReason = $"出庫日期 {revoke.OutboundDate.Value:yyyy/MM/dd} 已超過 3 天，無法取消";
                    continue;
                }

                revoke.Status = "成功";
                revoke.FailReason = string.Empty;
            }
        }

        /// <summary>
        /// 取消出庫資料
        /// </summary>
        /// <param name="successList">成功的資料列表</param>
        /// <param name="userName">使用者名稱</param>
        private void RevokeOutboundData(List<ShipmentOutboundRevokeModel> successList)
        {
            if (successList.Count == 0)
                return;

            var updateSql = @"
                UPDATE [jetf].[dbo].[ShipmentInbound]
                SET 
                    OutboundDate = NULL,
                    OutboundTrackingNo = NULL,
                    OutboundTime = NULL,
                    OutboundOpe = NULL,
                    WarehouseProcessType = NULL,
                    WarehouseProcessTime = NULL,
                    WarehouseProcessOpe = NULL
                WHERE Id = @Id";

            var insertHistorySql = @"
                INSERT INTO [jetf].[dbo].[ShipmentInboundEditHistory]
                ([ShipmentInboundId], [FieldName], [OldValue], [NewValue], [EditTime], [EditUser])
                VALUES
                (@ShipmentInboundId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)";

            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in successList)
                    {
                        conn.Execute(updateSql, new { Id = item.ShipmentInboundId }, transaction);

                        conn.Execute(insertHistorySql, new
                        {
                            ShipmentInboundId = item.ShipmentInboundId,
                            FieldName = "出庫日期",
                            OldValue = item.OutboundDate.HasValue ? item.OutboundDate.Value.ToString("yyyy/MM/dd") : string.Empty,
                            NewValue = string.Empty,
                            EditTime = DateTime.Now,
                            EditUser = GetUserId()
                        }, transaction);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            conn.Close();
        }
    }
}
