using Service.Data;
using Service.Services.FlightArrivalUpload.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Services.FlightArrivalUpload
{
    public partial class FlightArrivalUploadService
    {
        /// <summary>
        /// 在同一交易內寫入上傳資料及尚未存在的處置說明。
        /// </summary>
        /// <param name="data">已完成解析與驗證的匯入資料。</param>
        /// <param name="userId">上傳人員代碼。</param>
        /// <param name="today">供處理紀錄資料日期使用的上傳時間。</param>
        private void SaveUpload(FlightArrivalUploadData data, string userId, DateTime today)
        {
            var originalCommandTimeout = _jetfDb.Database.CommandTimeout;
            try
            {
                _jetfDb.Database.CommandTimeout = 600;
                using (var transaction = _jetfDb.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    SaveClearanceRows(data.ClearanceRows, userId);
                    SaveArrivalRows(data.ArrivalRows, userId);
                    SaveProcessRows(data.Processes, userId, today);
                    transaction.Commit();
                }
            }
            finally
            {
                _jetfDb.Database.CommandTimeout = originalCommandTimeout;
            }
        }

        /// <summary>
        /// 有 A03 或 B6F 資料時，依原流程清空上傳表，再使用 EF 批次新增。
        /// </summary>
        /// <param name="rows">待寫入的 A03、B6F 資料。</param>
        /// <param name="userId">上傳人員代碼。</param>
        private void SaveClearanceRows(List<FlightArrivalUploadClearanceModel> rows, string userId)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var entities = rows.Select(row => new A03B6FUploadEntity
            {
                DataType = row.DataType,
                MainNumber = row.MainNumber,
                BlNo = row.BlNo,
                Remark = row.Remark,
                BagCount = row.BagCount,
                UploadOpe = userId
            }).ToList();

            _jetfDb.Database.ExecuteSqlCommand("TRUNCATE TABLE [dbo].[A03_B6F_UPLOAD]");
            _jetfDb.BulkInsert(entities);
        }

        /// <summary>
        /// 有班機派件資料時，依原流程刪除同月份資料，再使用 EF 批次新增。
        /// </summary>
        /// <param name="rows">待寫入的班機派件資料，以第一筆資料的月份決定刪除範圍。</param>
        /// <param name="userId">上傳人員代碼。</param>
        private void SaveArrivalRows(List<FlightArrivalUploadArrivalModel> rows, string userId)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var entities = rows.Select(row => new ArriveUploadEntity
            {
                Month = row.Month,
                CustId = row.CustId,
                MainNumber = row.MainNumber,
                LineCode = ArrivalNullableValue(row.LineCode),
                FlightNumber = row.FlightNumber,
                FlightCount = ArrivalCountValue(row.FlightCount),
                ArriveDate1 = ArrivalDateValue(row.ArriveDate1),
                ArriveDate2 = ArrivalDateValue(row.ArriveDate2),
                ArriveDate3 = ArrivalDateValue(row.ArriveDate3),
                ArriveDate4 = ArrivalDateValue(row.ArriveDate4),
                ArriveDate5 = ArrivalDateValue(row.ArriveDate5),
                TransNo = row.TransNo,
                TransDate1 = ArrivalDateValue(row.TransDate1),
                TransDate2 = ArrivalDateValue(row.TransDate2),
                TransDate3 = ArrivalDateValue(row.TransDate3),
                TransDate4 = ArrivalDateValue(row.TransDate4),
                TransDate5 = ArrivalDateValue(row.TransDate5),
                TransDate6 = ArrivalDateValue(row.TransDate6),
                TransDate7 = ArrivalDateValue(row.TransDate7),
                TransDate8 = ArrivalDateValue(row.TransDate8),
                TransDate9 = ArrivalDateValue(row.TransDate9),
                TransDate10 = ArrivalDateValue(row.TransDate10),
                TransCount1 = ArrivalCountValue(row.TransCount1),
                TransCount2 = ArrivalCountValue(row.TransCount2),
                TransCount3 = ArrivalCountValue(row.TransCount3),
                TransCount4 = ArrivalCountValue(row.TransCount4),
                TransCount5 = ArrivalCountValue(row.TransCount5),
                TransCount6 = ArrivalCountValue(row.TransCount6),
                TransCount7 = ArrivalCountValue(row.TransCount7),
                TransCount8 = ArrivalCountValue(row.TransCount8),
                TransCount9 = ArrivalCountValue(row.TransCount9),
                TransCount10 = ArrivalCountValue(row.TransCount10),
                Ori = row.Ori,
                TransitAirport = row.TransitAirport,
                Dest = row.Dest,
                UploadOpe = userId
            }).ToList();

            _jetfDb.Database.ExecuteSqlCommand(
                "DELETE FROM [dbo].[ARRIVE_UPLOAD] WHERE MM = @MM",
                UploadParameter("@MM", rows[0].Month));
            _jetfDb.BulkInsert(entities);
        }


        /// <summary>
        /// 使用 EF 一次查出既有紀錄，在記憶體排除已存在的分提單號與處理類型後，再批次新增。
        /// </summary>
        /// <param name="rows">已完成同檔去重的處理紀錄；資料庫已有相同分提單號與處理類型時略過。</param>
        /// <param name="userId">上傳人員代碼。</param>
        /// <param name="today">供處理紀錄資料日期使用的上傳時間。</param>
        private void SaveProcessRows(IEnumerable<FlightArrivalUploadProcessModel> rows, string userId, DateTime today)
        {
            // 讀檔時已依分提單號及處理類型去重；沒有待新增資料時不查詢資料庫。
            var processRows = rows.ToList();
            if (processRows.Count == 0)
            {
                return;
            }

            // 查出既有單號及類型組合，包含已刪除紀錄，符合「已存在就不新增」的規則。
            // 使用既有擴充方法批次比對複合欄位，僅取回查重欄位並移除前後空白。
            var existingRows = _jetfDb.Processes.AsNoTracking()
                .WhereBulkContains(
                    _jetfDb,
                    processRows,
                    process => new { process.DlvInv, process.ProcessType },
                    row => new { row.DlvInv, ProcessType = row.ProcessType.ToString() },
                    process => new
                    {
                        DlvInv = process.DlvInv.Trim(),
                        ProcessType = process.ProcessType.Trim()
                    })
                .Distinct()
                .ToList();
            // 將查詢結果依分提單號整理成處理類型集合，供記憶體快速查找。
            var existingProcessTypes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in existingRows)
            {
                if (!existingProcessTypes.TryGetValue(row.DlvInv, out var types))
                {
                    types = new HashSet<string>(StringComparer.Ordinal);
                    existingProcessTypes.Add(row.DlvInv, types);
                }
                types.Add(row.ProcessType);
            }

            // 逐筆比對既有組合；同一分提單號的不同處理類型仍可分別新增。
            var entities = new List<ProcessEntity>();
            foreach (var row in processRows)
            {
                var processType = row.ProcessType.ToString();
                if (existingProcessTypes.TryGetValue(row.DlvInv, out var types) && types.Contains(processType))
                {
                    continue;
                }

                // 只建立尚未存在的紀錄，設定本次日期、人員，並沿用空白附件欄位。
                entities.Add(new ProcessEntity
                {
                    DataDate = today.ToString("yyyyMMdd"),
                    DlvInv = row.DlvInv,
                    Remark = row.Remark,
                    FilePath = string.Empty,
                    FileName = string.Empty,
                    UserId = userId,
                    ProcessType = processType
                });
            }

            // 全部篩選完成後，在呼叫端建立的同一交易內批次新增。
            if (entities.Count > 0)
            {
                _jetfDb.BulkInsert(entities);
            }
        }


        /// <summary>
        /// 沿用舊上傳流程以 NVARCHAR 參數傳入資料，避免將檔案內容組入 SQL。
        /// </summary>
        /// <param name="name">SQL 參數名稱。</param>
        /// <param name="value">參數值；null 會轉換為資料庫 NULL。</param>
        /// <returns>包含名稱、型別與值的 SQL 參數。</returns>
        private static SqlParameter UploadParameter(string name, object value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar)
            {
                Value = value ?? DBNull.Value
            };
        }

        /// <summary>
        /// 渠道代碼的空白欄位，依舊流程寫入資料庫 NULL。
        /// </summary>
        /// <param name="value">待寫入的渠道代碼。</param>
        /// <returns>原欄位值；null 或空字串轉換為 null。</returns>
        private static string ArrivalNullableValue(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// 將已驗證的班機或派件時間轉成日期，空白欄位依舊流程寫入 NULL。
        /// </summary>
        /// <param name="value">待寫入的班機到達時間或派件送達時間。</param>
        /// <returns>轉換後的日期；null 或空字串回傳 null。</returns>
        private static DateTime? ArrivalDateValue(string value)
        {
            return string.IsNullOrEmpty(value) ? (DateTime?)null : DateTime.Parse(value);
        }

        /// <summary>
        /// 袋數的空白欄位，依舊流程寫入零。
        /// </summary>
        /// <param name="value">待寫入的袋數。</param>
        /// <returns>轉換後的整數袋數；null 或空字串回傳零。</returns>
        private static int ArrivalCountValue(string value)
        {
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }
    }
}
