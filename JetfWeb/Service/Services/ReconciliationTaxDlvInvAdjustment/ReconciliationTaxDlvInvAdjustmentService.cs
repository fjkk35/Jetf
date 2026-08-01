using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.ReconciliationTaxDlvInvAdjustment.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Service.Services.ReconciliationTaxDlvInvAdjustment
{
    /// <summary>
    /// 稅金物流貨號調整服務。
    /// </summary>
    public sealed class ReconciliationTaxDlvInvAdjustmentService : _BaseService
    {
        private static readonly string[] RequiredHeaders =
        {
            "分提單號",
            "舊物流貨號",
            "新物流貨號"
        };

        /// <summary>
        /// 建立稅金物流貨號調整服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationTaxDlvInvAdjustmentService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳並調整稅金物流貨號。
        /// </summary>
        /// <param name="filePath">Excel 檔案實體路徑。</param>
        /// <returns>每列處理結果。</returns>
        public ResponseModel Upload(string filePath)
        {
            try
            {
                List<ReconciliationTaxDlvInvAdjustmentUploadRow> uploadRows;
                using (var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    uploadRows = ReadUploadRows(stream);
                }

                if (!uploadRows.Any())
                {
                    return new ResponseModel("Excel 檔案中沒有資料。");
                }

                ValidateRows(uploadRows);
                var validationErrorRows = uploadRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Status))
                    .ToList();
                if (validationErrorRows.Count > 0)
                {
                    var validationResult = SaveValidationFailureLogs(uploadRows);
                    validationResult.Data = validationErrorRows;

                    return new ResponseModel
                    {
                        IsSuccess = false,
                        status = Status.error,
                        msg = validationResult.Message,
                        ReturnObject = validationResult
                    };
                }

                var result = UpdateDlvInvs(uploadRows);

                return new ResponseModel
                {
                    IsSuccess = true,
                    status = Status.success,
                    msg = result.Message,
                    ReturnObject = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 產生稅金物流貨號調整 Excel 範例檔。
        /// </summary>
        /// <returns>Excel 檔案內容。</returns>
        public byte[] ExportTemplate()
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("稅金物流貨號調整範例");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, RequiredHeaders, headerStyle);

            var dataRow = sheet.CreateRow(1);
            NpoiCell.CreateCell(dataRow, 0, "T123456789", dataStyle);
            NpoiCell.CreateCell(dataRow, 1, "OLD123456", dataStyle);
            NpoiCell.CreateCell(dataRow, 2, "NEW123456", dataStyle);

            for (var index = 0; index < RequiredHeaders.Length; index++)
            {
                sheet.AutoSizeColumn(index);
                if (sheet.GetColumnWidth(index) < 3000)
                {
                    sheet.SetColumnWidth(index, 3000);
                }
            }

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 讀取 Excel 上傳資料。
        /// </summary>
        /// <param name="stream">Excel 檔案串流。</param>
        /// <returns>上傳列資料。</returns>
        private static List<ReconciliationTaxDlvInvAdjustmentUploadRow> ReadUploadRows(
            Stream stream)
        {
            var uploadRows = new List<ReconciliationTaxDlvInvAdjustmentUploadRow>();
            IWorkbook workbook = new XSSFWorkbook(stream);
            var sheet = workbook.GetSheetAt(0);
            var headerRowIndex = FindHeaderRowIndex(sheet);
            var columnIndexes = GetColumnIndexes(sheet.GetRow(headerRowIndex));

            for (var rowIndex = headerRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var excelRow = sheet.GetRow(rowIndex);
                if (excelRow == null)
                {
                    continue;
                }

                var uploadRow = new ReconciliationTaxDlvInvAdjustmentUploadRow
                {
                    RowNo = rowIndex + 1,
                    TrackingNo = excelRow.GetCellData(columnIndexes["分提單號"]).Trim(),
                    OldDlvInv = excelRow.GetCellData(columnIndexes["舊物流貨號"]).Trim(),
                    NewDlvInv = excelRow.GetCellData(columnIndexes["新物流貨號"]).Trim()
                };

                if (string.IsNullOrWhiteSpace(uploadRow.TrackingNo) &&
                    string.IsNullOrWhiteSpace(uploadRow.OldDlvInv) &&
                    string.IsNullOrWhiteSpace(uploadRow.NewDlvInv))
                {
                    continue;
                }

                uploadRows.Add(uploadRow);
            }

            return uploadRows;
        }

        /// <summary>
        /// 尋找包含完整必要欄位的表頭列。
        /// </summary>
        /// <param name="sheet">Excel 工作表。</param>
        /// <returns>表頭列索引。</returns>
        private static int FindHeaderRowIndex(ISheet sheet)
        {
            var lastRowIndex = Math.Min(sheet.LastRowNum, 20);
            for (var rowIndex = 0; rowIndex <= lastRowIndex; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var headers = Enumerable.Range(
                        0,
                        Math.Max(0, (int)row.LastCellNum))
                    .Select(row.GetCellData)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (RequiredHeaders.All(headers.Contains))
                {
                    return rowIndex;
                }
            }

            throw new InvalidDataException(
                "找不到完整表頭，請確認包含：分提單號、舊物流貨號、新物流貨號。");
        }

        /// <summary>
        /// 取得必要欄位的 Excel 欄位索引。
        /// </summary>
        /// <param name="headerRow">Excel 表頭列。</param>
        /// <returns>欄位名稱與欄位索引。</returns>
        private static Dictionary<string, int> GetColumnIndexes(IRow headerRow)
        {
            var columnIndexes = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            for (var columnIndex = 0; columnIndex < headerRow.LastCellNum; columnIndex++)
            {
                var header = headerRow.GetCellData(columnIndex);
                if (RequiredHeaders.Contains(header) &&
                    !columnIndexes.ContainsKey(header))
                {
                    columnIndexes.Add(header, columnIndex);
                }
            }

            return columnIndexes;
        }

        /// <summary>
        /// 驗證必填欄位及檔案內重複資料。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        private static void ValidateRows(
            List<ReconciliationTaxDlvInvAdjustmentUploadRow> uploadRows)
        {
            foreach (var row in uploadRows)
            {
                var failReasons = new List<string>();
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    failReasons.Add("分提單號必填");
                }

                if (string.IsNullOrWhiteSpace(row.OldDlvInv))
                {
                    failReasons.Add("舊物流貨號必填");
                }

                if (string.IsNullOrWhiteSpace(row.NewDlvInv))
                {
                    failReasons.Add("新物流貨號必填");
                }

                row.Status = string.Join("；", failReasons);
            }

            var duplicateGroups = uploadRows
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.TrackingNo) &&
                    !string.IsNullOrWhiteSpace(x.OldDlvInv))
                .GroupBy(x => new
                {
                    TrackingNo = x.TrackingNo.ToUpperInvariant(),
                    OldDlvInv = x.OldDlvInv.ToUpperInvariant()
                })
                .Where(x => x.Count() > 1);

            foreach (var duplicateGroup in duplicateGroups)
            {
                foreach (var row in duplicateGroup)
                {
                    row.Status = string.IsNullOrWhiteSpace(row.Status)
                        ? "分提單號及舊物流貨號重複"
                        : row.Status + "；分提單號及舊物流貨號重複";
                }
            }
        }

        /// <summary>
        /// 批次更新費用主檔及費用明細的物流貨號，並寫入上傳紀錄。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <returns>上傳結果。</returns>
        private ReconciliationTaxDlvInvAdjustmentUploadResult UpdateDlvInvs(
            List<ReconciliationTaxDlvInvAdjustmentUploadRow> uploadRows)
        {
            // Step 1：一次查出符合「分提單號＋舊物流貨號」的費用主檔與費用明細。
            var feeMasters = JetfDb.FeeMasters
                .WhereBulkContains(
                    JetfDb,
                    uploadRows,
                    entity => new { entity.TrackingNo, entity.DlvInv },
                    row => new { row.TrackingNo, DlvInv = row.OldDlvInv })
                .ToList();
            var feeMasterDetails = JetfDb.FeeMasterDetails
                .WhereBulkContains(
                    JetfDb,
                    uploadRows,
                    entity => new { entity.TrackingNo, entity.DlvInv },
                    row => new { row.TrackingNo, DlvInv = row.OldDlvInv })
                .ToList();

            // Step 2：判斷每列是否命中資料，並將符合資料的物流貨號改成新值。
            foreach (var row in uploadRows)
            {
                var matchedFeeMasters = feeMasters
                    .Where(x => string.Equals(x.TrackingNo.Trim(), row.TrackingNo, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(x.DlvInv.Trim(), row.OldDlvInv, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var matchedFeeMasterDetails = feeMasterDetails
                    .Where(x => string.Equals(x.TrackingNo.Trim(), row.TrackingNo, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(x.DlvInv.Trim(), row.OldDlvInv, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                row.IsSuccess = matchedFeeMasters.Any() || matchedFeeMasterDetails.Any();
                row.Status = row.IsSuccess ? "更新成功" : "查無資料";

                //更新物流貨號
                matchedFeeMasters.ForEach(x => x.DlvInv = row.NewDlvInv);
                matchedFeeMasterDetails.ForEach(x => x.DlvInv = row.NewDlvInv);
            }

            var currentTime = DateTime.Now;
            var currentUserId = GetUserId();
            var logEntities = CreateLogEntities(
                uploadRows,
                currentUserId,
                currentTime);

            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    // Step 3：以批次方式更新兩張費用表，避免逐筆送出 UPDATE。
                    if (feeMasters.Any())
                    {
                        JetfDb.BulkUpdate(feeMasters);
                    }

                    if (feeMasterDetails.Any())
                    {
                        JetfDb.BulkUpdate(feeMasterDetails);
                    }

                    // Step 4：成功、查無資料及欄位錯誤都寫入 LOG，保留完整上傳軌跡。
                    JetfDb.BulkInsert(logEntities);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return new ReconciliationTaxDlvInvAdjustmentUploadResult
            {
                Count = uploadRows.Count,
                UpdatedCount = uploadRows.Count(x => x.IsSuccess),
                FailCount = uploadRows.Count(x => !x.IsSuccess),
                Message = "上傳完成",
                Data = uploadRows
                    .OrderByDescending(x => x.IsSuccess)
                    .ThenBy(x => x.RowNo)
                    .ToList()
            };
        }

        /// <summary>
        /// 儲存驗證失敗的整批上傳 LOG，不查詢或更新費用資料。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <returns>整批失敗結果。</returns>
        private ReconciliationTaxDlvInvAdjustmentUploadResult SaveValidationFailureLogs(
            List<ReconciliationTaxDlvInvAdjustmentUploadRow> uploadRows)
        {
            var currentTime = DateTime.Now;
            var currentUserId = GetUserId() ?? "system";
            currentUserId = currentUserId.Length > 10
                ? currentUserId.Substring(0, 10)
                : currentUserId;
            var logEntities = CreateLogEntities(
                uploadRows,
                currentUserId,
                currentTime);

            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    JetfDb.BulkInsert(logEntities);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return new ReconciliationTaxDlvInvAdjustmentUploadResult
            {
                Count = uploadRows.Count,
                UpdatedCount = 0,
                FailCount = uploadRows.Count,
                Message = "驗證失敗，整批未更新",
                Data = uploadRows
            };
        }

        /// <summary>
        /// 建立上傳 LOG Entity。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <param name="currentUserId">目前操作人員。</param>
        /// <param name="currentTime">目前時間。</param>
        /// <returns>LOG Entity 清單。</returns>
        private static List<FeeMasterDlvInvModifyEntity> CreateLogEntities(
            IEnumerable<ReconciliationTaxDlvInvAdjustmentUploadRow> uploadRows,
            string currentUserId,
            DateTime currentTime)
        {
            return uploadRows
                .Select(row => new FeeMasterDlvInvModifyEntity
                {
                    TrackingNo = row.TrackingNo ?? string.Empty,
                    OldDlvInv = row.OldDlvInv ?? string.Empty,
                    NewDlvInv = row.NewDlvInv ?? string.Empty,
                    IsSuccess = row.IsSuccess,
                    CreatedUserId = currentUserId,
                    CreatedTime = currentTime
                })
                .ToList();
        }
    }
}
