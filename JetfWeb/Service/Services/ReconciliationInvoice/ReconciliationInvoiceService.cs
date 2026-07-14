using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.ReconciliationInvoice.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ReconciliationInvoice
{
    /// <summary>
    /// 代收銷帳作業服務。
    /// </summary>
    public class ReconciliationInvoiceService : _BaseService
    {
        /// <summary>
        /// 建立代收銷帳服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationInvoiceService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳代收銷帳發票資料。
        /// </summary>
        /// <param name="filePath">Excel 檔案路徑。</param>
        /// <returns>上傳結果。</returns>
        public ResponseModel UploadInvoices(string filePath)
        {
            try
            {
                var uploadRows = ReadExcelFile(filePath);
                if (uploadRows.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                ValidateRows(uploadRows);
                ValidateFeeMasterRows(uploadRows);

                var failRows = uploadRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.FailReason))
                    .ToList();

                if (failRows.Any())
                {
                    var failResult = new ReconciliationInvoiceUploadResult
                    {
                        Count = uploadRows.Count,
                        FailCount = failRows.Count,
                        Message = $"檔案共有 {uploadRows.Count} 筆資料，失敗 {failRows.Count} 筆，整批未寫入資料庫",
                        Data = failRows
                    };

                    return new ResponseModel
                    {
                        IsSuccess = false,
                        status = Status.error,
                        msg = failResult.Message,
                        ReturnObject = failResult
                    };
                }

                var successResult = UpsertInvoices(uploadRows);
                return new ResponseModel
                {
                    IsSuccess = true,
                    status = Status.success,
                    msg = successResult.Message,
                    ReturnObject = successResult
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 讀取 Excel 檔案內容。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <returns>上傳列資料。</returns>
        private List<ReconciliationInvoiceUploadRow> ReadExcelFile(string filePath)
        {
            var uploadRows = new List<ReconciliationInvoiceUploadRow>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    var uploadRow = NormalizeRow(new ReconciliationInvoiceUploadRow
                    {
                        RowNo = i + 1,
                        InvoiceType = row.GetCellData(0),
                        InvoiceDateText = row.GetCellData(1),
                        InvoiceNo = row.GetCellData(2),
                        TrackingNo = row.GetCellData(3),
                        DlvInv = row.GetCellData(4)
                    });

                    if (IsEmptyRow(uploadRow))
                    {
                        continue;
                    }

                    uploadRows.Add(uploadRow);
                }
            }

            return uploadRows;
        }

        /// <summary>
        /// 正規化單筆上傳資料。
        /// </summary>
        /// <param name="row">原始資料。</param>
        /// <returns>正規化後資料。</returns>
        private static ReconciliationInvoiceUploadRow NormalizeRow(ReconciliationInvoiceUploadRow row)
        {
            row.InvoiceType = row.InvoiceType?.Trim() ?? string.Empty;
            row.InvoiceDateText = row.InvoiceDateText?.Trim() ?? string.Empty;
            row.InvoiceNo = row.InvoiceNo?.Trim() ?? string.Empty;
            row.TrackingNo = row.TrackingNo?.Trim() ?? string.Empty;
            row.DlvInv = row.DlvInv?.Trim() ?? string.Empty;
            row.FailReason = row.FailReason?.Trim() ?? string.Empty;
            return row;
        }

        /// <summary>
        /// 判斷是否為空白列。
        /// </summary>
        /// <param name="row">列資料。</param>
        /// <returns>是否空白。</returns>
        private static bool IsEmptyRow(ReconciliationInvoiceUploadRow row)
        {
            return string.IsNullOrWhiteSpace(row.InvoiceType)
                && string.IsNullOrWhiteSpace(row.InvoiceDateText)
                && string.IsNullOrWhiteSpace(row.InvoiceNo)
                && string.IsNullOrWhiteSpace(row.TrackingNo)
                && string.IsNullOrWhiteSpace(row.DlvInv);
        }

        /// <summary>
        /// 驗證上傳資料。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        private static void ValidateRows(List<ReconciliationInvoiceUploadRow> uploadRows)
        {
            foreach (var row in uploadRows)
            {
                var failReasons = new List<string>();

                if (string.IsNullOrWhiteSpace(row.InvoiceType))
                {
                    failReasons.Add("發票類別必填");
                }

                if (string.IsNullOrWhiteSpace(row.InvoiceDateText))
                {
                    failReasons.Add("開立日期必填");
                }
                else if (DateTime.TryParse(row.InvoiceDateText, out DateTime invoiceDate))
                {
                    row.InvoiceDate = invoiceDate.Date;
                }
                else
                {
                    failReasons.Add("開立日期格式錯誤");
                }

                if (string.IsNullOrWhiteSpace(row.InvoiceNo))
                {
                    failReasons.Add("發票號碼必填");
                }

                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    failReasons.Add("分提單號必填");
                }

                if (string.IsNullOrWhiteSpace(row.DlvInv))
                {
                    failReasons.Add("物流貨號必填");
                }

                row.FailReason = string.Join("；", failReasons);
            }

            var duplicateGroups = uploadRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo)
                    && !string.IsNullOrWhiteSpace(x.DlvInv))
                .GroupBy(x => new
                {
                    TrackingNo = x.TrackingNo.ToUpperInvariant(),
                    DlvInv = x.DlvInv.ToUpperInvariant()
                })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicateGroups)
            {
                foreach (var row in group)
                {
                    AppendFailReason(row, "分提單號及物流貨號重複");
                }
            }
        }

        /// <summary>
        /// 驗證分提單號及物流貨號是否存在於費用主檔。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        private void ValidateFeeMasterRows(List<ReconciliationInvoiceUploadRow> uploadRows)
        {
            var rowsWithKey = uploadRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo)
                    && !string.IsNullOrWhiteSpace(x.DlvInv))
                .ToList();

            if (!rowsWithKey.Any())
            {
                return;
            }

            var feeMasterRows = JetfDb.FeeMasters
                .AsNoTracking()
                .WhereBulkContains(
                    JetfDb,
                    rowsWithKey,
                    x => new { x.TrackingNo, x.DlvInv },
                    x => new { x.TrackingNo, x.DlvInv });

            var feeMasterKeys = feeMasterRows.ToLookup(x => new
            {
                TrackingNo = (x.TrackingNo ?? string.Empty).Trim().ToUpperInvariant(),
                DlvInv = (x.DlvInv ?? string.Empty).Trim().ToUpperInvariant()
            });

            foreach (var row in rowsWithKey)
            {
                var key = new
                {
                    TrackingNo = row.TrackingNo.ToUpperInvariant(),
                    DlvInv = row.DlvInv.ToUpperInvariant()
                };

                if (!feeMasterKeys.Contains(key))
                {
                    AppendFailReason(row, "稅金檔查無資料（分提單號 + 物流貨號）");
                }
            }
        }

        /// <summary>
        /// 附加失敗原因。
        /// </summary>
        /// <param name="row">上傳列資料。</param>
        /// <param name="reason">失敗原因。</param>
        private static void AppendFailReason(ReconciliationInvoiceUploadRow row, string reason)
        {
            if (string.IsNullOrWhiteSpace(row.FailReason))
            {
                row.FailReason = reason;
                return;
            }

            if (row.FailReason.IndexOf(reason, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            row.FailReason += "；" + reason;
        }

        /// <summary>
        /// 寫入或更新代收銷帳發票資料。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <returns>寫入結果。</returns>
        private ReconciliationInvoiceUploadResult UpsertInvoices(List<ReconciliationInvoiceUploadRow> uploadRows)
        {
            var currentUserId = GetUserId() ?? "system";
            var currentTime = DateTime.Now;

            var existingEntities = JetfDb.ReconciliationInvoices
                .WhereBulkContains(
                    JetfDb,
                    uploadRows,
                    x => new { x.TrackingNo, x.DlvInv },
                    x => new { x.TrackingNo, x.DlvInv });

            var existingKeys = existingEntities.ToLookup(x => new
            {
                TrackingNo = (x.TrackingNo ?? string.Empty).Trim().ToUpperInvariant(),
                DlvInv = (x.DlvInv ?? string.Empty).Trim().ToUpperInvariant()
            });

            var createdCount = uploadRows.Count(x => !existingKeys.Contains(new
            {
                TrackingNo = x.TrackingNo.ToUpperInvariant(),
                DlvInv = x.DlvInv.ToUpperInvariant()
            }));
            var updatedCount = uploadRows.Count - createdCount;
            var insertEntities = uploadRows
                .Select(row => new ReconciliationInvoiceEntity
                {
                    Type = row.InvoiceType,
                    Date = row.InvoiceDate.GetValueOrDefault(),
                    Invoice = row.InvoiceNo,
                    TrackingNo = row.TrackingNo,
                    DlvInv = row.DlvInv,
                    CreatedOpe = currentUserId,
                    CreatedTime = currentTime
                })
                .ToList();

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    var existingIds = existingEntities
                        .Select(x => x.Id)
                        .Distinct()
                        .ToList();

                    if (existingIds.Any())
                    {
                        JetfDb.DeleteByColumnValues<ReconciliationInvoiceEntity, int>(
                            existingIds,
                            x => x.Id);
                    }

                    if (insertEntities.Any())
                    {
                        JetfDb.BulkInsert(insertEntities);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return new ReconciliationInvoiceUploadResult
            {
                Count = uploadRows.Count,
                CreatedCount = createdCount,
                UpdatedCount = updatedCount,
                Message = $"上傳完成，共 {uploadRows.Count} 筆，新增 {createdCount} 筆，更新 {updatedCount} 筆"
            };
        }

    }
}
