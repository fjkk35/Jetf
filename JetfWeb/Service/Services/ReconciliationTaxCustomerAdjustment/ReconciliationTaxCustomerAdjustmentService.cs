using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ReconciliationTaxCustomerAdjustment.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Service.Services.ReconciliationTaxCustomerAdjustment
{
    /// <summary>
    /// 稅金客戶調整服務。
    /// </summary>
    public sealed class ReconciliationTaxCustomerAdjustmentService : _BaseService
    {
        private static readonly string[] RequiredHeaders =
        {
            "分提單號",
            "物流貨號",
            "新客戶代號"
        };

        /// <summary>
        /// 建立稅金客戶調整服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationTaxCustomerAdjustmentService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳並調整費用主檔客戶代號。
        /// </summary>
        /// <param name="filePath">Excel 檔案實體路徑。</param>
        /// <returns>每列處理結果。</returns>
        public ResponseModel Upload(string filePath)
        {
            try
            {
                List<ReconciliationTaxCustomerAdjustmentUploadRow> uploadRows;
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
                if (validationErrorRows.Any())
                {
                    var validationResult = new ReconciliationTaxCustomerAdjustmentUploadResult
                    {
                        Count = uploadRows.Count,
                        UpdatedCount = 0,
                        FailCount = uploadRows.Count,
                        Message = "驗證失敗，整批未更新",
                        Data = validationErrorRows
                    };

                    return new ResponseModel
                    {
                        IsSuccess = false,
                        status = Status.error,
                        msg = validationResult.Message,
                        ReturnObject = validationResult
                    };
                }

                var result = UpdateCustomers(uploadRows);
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
        /// 產生稅金客戶調整 Excel 範例檔。
        /// </summary>
        /// <returns>Excel 檔案內容。</returns>
        public byte[] ExportTemplate()
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("稅金客戶調整範例");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            NpoiCell.CreateHeaderCells(
                sheet.CreateRow(0),
                RequiredHeaders,
                headerStyle);

            var dataRow = sheet.CreateRow(1);
            NpoiCell.CreateCell(dataRow, 0, "T123456789", dataStyle);
            NpoiCell.CreateCell(dataRow, 1, "DLV123456", dataStyle);
            NpoiCell.CreateCell(dataRow, 2, "00001", dataStyle);

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
        private static List<ReconciliationTaxCustomerAdjustmentUploadRow> ReadUploadRows(
            Stream stream)
        {
            var uploadRows = new List<ReconciliationTaxCustomerAdjustmentUploadRow>();
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

                var uploadRow = new ReconciliationTaxCustomerAdjustmentUploadRow
                {
                    RowNo = rowIndex + 1,
                    TrackingNo = excelRow.GetCellData(columnIndexes["分提單號"]).Trim(),
                    DlvInv = excelRow.GetCellData(columnIndexes["物流貨號"]).Trim(),
                    NewCustomerCode = excelRow.GetCellData(columnIndexes["新客戶代號"]).Trim()
                };

                if (string.IsNullOrWhiteSpace(uploadRow.TrackingNo) &&
                    string.IsNullOrWhiteSpace(uploadRow.DlvInv) &&
                    string.IsNullOrWhiteSpace(uploadRow.NewCustomerCode))
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
                "找不到完整表頭，請確認包含：分提單號、物流貨號、新客戶代號。");
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
            List<ReconciliationTaxCustomerAdjustmentUploadRow> uploadRows)
        {
            foreach (var row in uploadRows)
            {
                var failReasons = new List<string>();
                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    failReasons.Add("分提單號必填");
                }

                if (string.IsNullOrWhiteSpace(row.DlvInv))
                {
                    failReasons.Add("物流貨號必填");
                }

                if (string.IsNullOrWhiteSpace(row.NewCustomerCode))
                {
                    failReasons.Add("新客戶代號必填");
                }

                row.Status = string.Join("；", failReasons);
            }

            var duplicateGroups = uploadRows
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.TrackingNo) &&
                    !string.IsNullOrWhiteSpace(x.DlvInv))
                .GroupBy(x => new
                {
                    TrackingNo = x.TrackingNo.ToUpperInvariant(),
                    DlvInv = x.DlvInv.ToUpperInvariant()
                })
                .Where(x => x.Count() > 1);

            foreach (var duplicateGroup in duplicateGroups)
            {
                foreach (var row in duplicateGroup)
                {
                    row.Status = string.IsNullOrWhiteSpace(row.Status)
                        ? "分提單號及物流貨號重複"
                        : row.Status + "；分提單號及物流貨號重複";
                }
            }
        }

        /// <summary>
        /// 批次查詢費用主檔、驗證客戶代號、更新客戶並寫入 LOG。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <returns>上傳結果。</returns>
        private ReconciliationTaxCustomerAdjustmentUploadResult UpdateCustomers(
            List<ReconciliationTaxCustomerAdjustmentUploadRow> uploadRows)
        {
            // Step 1：依分提單號及物流貨號一次查出費用主檔。
            var feeMasters = JetfDb.FeeMasters
                .WhereBulkContains(
                    JetfDb,
                    uploadRows,
                    entity => new { entity.TrackingNo, entity.DlvInv },
                    row => new { row.TrackingNo, row.DlvInv })
                .ToList();

            // Step 2：一次取得空運及海運客戶代號，後續依費用主檔來源驗證。
            var airCustomerCodes = GetCustomerCodes(CustomerType.AIR, true);
            var seaCustomerCodes = GetCustomerCodes(CustomerType.SEA, false);
            var updatedFeeMasters = new List<FeeMasterEntity>();

            // Step 3：比對資料並只更新客戶代號正確的費用主檔。
            foreach (var row in uploadRows)
            {
                var matchedFeeMasters = feeMasters
                    .Where(x => x.TrackingNo == row.TrackingNo &&
                                x.DlvInv == row.DlvInv)
                    .ToList();

                if (!matchedFeeMasters.Any())
                {
                    row.IsSuccess = false;
                    row.Status = "查無資料";
                    continue;
                }

                var customerCodeIsValid = matchedFeeMasters.All(master =>
                    IsCustomerCodeValid(
                        master.Source,
                        row.NewCustomerCode,
                        airCustomerCodes,
                        seaCustomerCodes));
                if (!customerCodeIsValid)
                {
                    row.IsSuccess = false;
                    row.Status = "客戶代號不正確";
                    continue;
                }

                foreach (var master in matchedFeeMasters)
                {
                    master.Customer = row.NewCustomerCode;
                    updatedFeeMasters.Add(master);
                }

                row.IsSuccess = true;
                row.Status = "更新成功";
            }

            var logEntities = CreateLogEntities(
                uploadRows,
                GetCurrentUserId(),
                DateTime.Now);

            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    // Step 4：批次更新費用主檔並寫入所有上傳資料 LOG。
                    if (updatedFeeMasters.Any())
                    {
                        JetfDb.BulkUpdate(updatedFeeMasters);
                    }

                    JetfDb.BulkInsert(logEntities);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            var updatedCount = uploadRows.Count(x => x.IsSuccess);
            return new ReconciliationTaxCustomerAdjustmentUploadResult
            {
                Count = uploadRows.Count,
                UpdatedCount = updatedCount,
                FailCount = uploadRows.Count - updatedCount,
                Message = "上傳完成",
                Data = uploadRows
                    .OrderByDescending(x => x.IsSuccess)
                    .ThenBy(x => x.RowNo)
                    .ToList()
            };
        }

        /// <summary>
        /// 取得指定類型的客戶代號集合。
        /// </summary>
        /// <param name="customerType">客戶類型。</param>
        /// <param name="useOldCode">是否使用空運舊代號。</param>
        /// <returns>客戶代號集合。</returns>
        private HashSet<string> GetCustomerCodes(
            CustomerType customerType,
            bool useOldCode)
        {
            var customers = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == customerType.ToString());
            var codes = useOldCode
                ? customers
                    .Where(x => !string.IsNullOrEmpty(x.OldCode))
                    .Select(x => x.OldCode)
                : customers
                    .Where(x => !string.IsNullOrEmpty(x.CustCode))
                    .Select(x => x.CustCode);

            return new HashSet<string>(codes
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依費用主檔來源判斷客戶代號是否有效。
        /// </summary>
        /// <param name="source">費用主檔資料來源。</param>
        /// <param name="customerCode">待驗證客戶代號。</param>
        /// <param name="airCustomerCodes">空運客戶代號。</param>
        /// <param name="seaCustomerCodes">海運客戶代號。</param>
        /// <returns>客戶代號是否有效。</returns>
        private static bool IsCustomerCodeValid(
            string source,
            string customerCode,
            ISet<string> airCustomerCodes,
            ISet<string> seaCustomerCodes)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
            {
                return false;
            }

            var normalizedCode = customerCode.Trim();
            return IsAirSource(source)
                ? airCustomerCodes.Contains(normalizedCode)
                : seaCustomerCodes.Contains(normalizedCode);
        }

        /// <summary>
        /// 判斷費用主檔資料來源是否為空運來源。
        /// </summary>
        /// <param name="source">費用主檔資料來源。</param>
        /// <returns>是否為 TACT 或 FTZ。</returns>
        private static bool IsAirSource(string source)
        {
            var value = (source ?? string.Empty).Trim();
            return string.Equals(value, "TACT", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "FTZ", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 建立目前使用者代號，並限制為資料表欄位長度。
        /// </summary>
        /// <returns>使用者代號。</returns>
        private string GetCurrentUserId()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "system";
            }

            return userId.Length > 10 ? userId.Substring(0, 10) : userId;
        }

        /// <summary>
        /// 建立上傳 LOG Entity。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <param name="currentUserId">目前操作人員。</param>
        /// <param name="currentTime">目前時間。</param>
        /// <returns>LOG Entity 清單。</returns>
        private static List<FeeMasterCustomerModifyEntity> CreateLogEntities(
            IEnumerable<ReconciliationTaxCustomerAdjustmentUploadRow> uploadRows,
            string currentUserId,
            DateTime currentTime)
        {
            return uploadRows
                .Select(row => new FeeMasterCustomerModifyEntity
                {
                    TrackingNo = row.TrackingNo ?? string.Empty,
                    DlvInv = row.DlvInv ?? string.Empty,
                    NewCustomerCode = row.NewCustomerCode ?? string.Empty,
                    IsSuccess = row.IsSuccess,
                    CreatedUserId = currentUserId,
                    CreatedTime = currentTime
                })
                .ToList();
        }
    }
}
