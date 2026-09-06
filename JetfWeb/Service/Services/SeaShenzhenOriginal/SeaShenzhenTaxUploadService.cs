using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞深圳稅單上傳服務。
    /// </summary>
    public class SeaShenzhenTaxUploadService : _BaseService
    {
        /// <summary>
        /// 初始化新遞深圳稅單上傳服務。
        /// </summary>
        public SeaShenzhenTaxUploadService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳新遞深圳稅單資料並轉入深圳稅金主檔。
        /// </summary>
        public ResponseModel Upload(string filePath, DateTime dataDate, SeaShenzhenTaxDataType dataType)
        {
            try
            {
                var dataDateText = dataDate.ToString("yyyyMMdd");
                var dataTypeText = dataType.ToDescription();

                // Step1: 依報關行設定讀取 Excel 與欄位定義。
                var definition = GetHeaderDefinition(dataType);
                var uploadRows = ReadExcelFile(filePath, definition);

                if (!uploadRows.Any())
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                // Step2: 驗證每筆上傳資料，拆分成功與失敗明細。
                ValidateUploadRows(uploadRows, definition);

                var failList = uploadRows.Where(x => x.UploadStatus == "失敗").ToList();
                var successRows = uploadRows.Where(x => x.UploadStatus != "失敗").ToList();

                if (!successRows.Any())
                {
                    var failMessage = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，請修正後重新上傳";
                    return new ResponseModel
                    {
                        status = Status.error,
                        msg = failMessage,
                        ReturnObject = new SeaShenzhenTaxUploadResult
                        {
                            DataDate = dataDateText,
                            DataType = dataTypeText,
                            SourceCount = uploadRows.Count,
                            SavedCount = 0,
                            DeletedCount = 0,
                            CreatedCount = 0,
                            FailCount = failList.Count,
                            ExceptionCount = 0,
                            Message = failMessage,
                            Data = failList
                        }
                    };
                }

                // Step3: 將成功資料寫入上傳檔與深圳稅金主檔。
                var result = SaveUploadRows(successRows, dataDateText, dataType, dataTypeText);

                // Step4: 合併失敗明細與轉檔結果，回傳畫面摘要。
                result.SourceCount = uploadRows.Count;
                result.FailCount = failList.Count;
                result.Data = failList;
                result.Message = BuildResultMessage(result);

                return new ResponseModel
                {
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
        /// 匯出新遞深圳稅金轉檔異常明細。
        /// </summary>
        public byte[] ExportTransferExceptions(IEnumerable<SeaShenzhenTaxTransferExceptionRow> exceptions)
        {
            var rows = (exceptions ?? Enumerable.Empty<SeaShenzhenTaxTransferExceptionRow>()).ToList();
            if (rows.Count == 0)
            {
                throw new Exception("查無異常明細");
            }

            var workbook = CreateTransferExceptionWorkbook(rows);
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 將驗證成功的稅單資料寫入 SeaShenzhenTax，並同步轉入深圳稅金主檔。
        /// </summary>
        private SeaShenzhenTaxUploadResult SaveUploadRows(
            List<SeaShenzhenTaxUploadRow> uploadRows,
            string dataDate,
            SeaShenzhenTaxDataType dataTypeValue,
            string dataType)
        {
            var now = DateTime.Now;
            var userId = GetUserId();

            // Step3-1: 先將成功列轉成 SeaShenzhenTax 實體。
            var taxEntities = uploadRows
                .Select(row => new SeaShenzhenTaxEntity
                {
                    DataDate = dataDate,
                    DataType = dataTypeValue,
                    TrackingNo = row.TrackingNo.Trim(),
                    Cod = row.Cod ?? 0,
                    Tax = row.Tax ?? 0,
                    Fee = row.Fee ?? 0,
                    CreatedUser = userId,
                    CreatedTime = now
                })
                .ToList();

            // Step3-2: 依分號彙總稅額，建立深圳稅金主檔與異常明細。
            var originalLookup = SeaShenzhenFeeTransferShared.GetOriginalLookup(JetfDb, taxEntities.Select(x => x.TrackingNo));
            var transferRows = new List<ShenzhenFeeMasterEntity>();
            var exceptions = new List<SeaShenzhenTaxTransferExceptionRow>();

            foreach (var taxGroup in taxEntities.GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase))
            {
                var totalTax = taxGroup.Sum(x => x.Tax);
                var cod = taxGroup
                    .Where(x => x.Cod > 0)
                    .Select(x => (int?)x.Cod)
                    .FirstOrDefault();
                var fee = taxGroup.Sum(x => x.Fee);
                SeaShenzhenOriginalEntity original;
                if (!originalLookup.TryGetValue(taxGroup.Key, out original))
                {
                    exceptions.AddRange(taxGroup.Select(x => CreateExceptionRow(x, "找不到託運單資料")));
                    continue;
                }

                transferRows.Add(SeaShenzhenFeeTransferShared.CreateTransferRow(
                    original,
                    totalTax,
                    dataDate,
                    null,
                    dataTypeValue,
                    null,
                    userId,
                    now,
                    cod,
                    fee));
            }

            int deletedCount;
            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    // Step3-3: 先清除同批次舊資料，再批次寫入新資料並回填關聯 Id。
                    DeleteExistingUploadRows(dataDate, dataTypeValue);
                    deletedCount = DeleteExistingTransferRows(dataDate, dataTypeValue);

                    if (transferRows.Count > 0)
                    {
                        JetfDb.BulkInsert(transferRows, operation => operation.AutoMapOutputDirection = true);
                        FillShenzhenFeeMasterIds(taxEntities, transferRows);
                    }

                    if (taxEntities.Count > 0)
                    {
                        JetfDb.BulkInsert(taxEntities);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return new SeaShenzhenTaxUploadResult
            {
                DataDate = dataDate,
                DataType = dataType,
                SourceCount = uploadRows.Count,
                SavedCount = taxEntities.Count,
                DeletedCount = deletedCount,
                CreatedCount = transferRows.Count,
                FailCount = 0,
                ExceptionCount = exceptions.Count,
                Exceptions = exceptions
            };
        }

        /// <summary>
        /// 建立上傳成功後回傳給畫面的摘要訊息。
        /// </summary>
        private static string BuildResultMessage(SeaShenzhenTaxUploadResult result)
        {
            return "轉入成功";
        }

        /// <summary>
        /// 以批次刪除移除同資料日期、同資料類型的既有上傳稅單資料。
        /// </summary>
        private int DeleteExistingUploadRows(string dataDate, SeaShenzhenTaxDataType dataType)
        {
            return JetfDb.DeleteWhere(JetfDb.SeaShenzhenTaxes
                .Where(x => x.DataDate == dataDate && x.DataType == dataType));
        }

        /// <summary>
        /// 以批次刪除移除同資料日期、同資料類型的既有轉檔資料。
        /// </summary>
        private int DeleteExistingTransferRows(string dataDate, SeaShenzhenTaxDataType dataType)
        {
            return JetfDb.DeleteWhere(JetfDb.ShenzhenFeeMasters
                .Where(x => x.DataDate == dataDate && x.DataType == dataType));
        }

        /// <summary>
        /// 將轉檔主檔 Id 回寫到同分號的稅單資料。
        /// </summary>
        private static void FillShenzhenFeeMasterIds(
            IEnumerable<SeaShenzhenTaxEntity> taxEntities,
            IEnumerable<ShenzhenFeeMasterEntity> transferRows)
        {
            var transferIdLookup = transferRows
                .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => (int?)group.First().Id,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var taxEntity in taxEntities.Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo)))
            {
                int? shenzhenFeeMasterId;
                if (transferIdLookup.TryGetValue(taxEntity.TrackingNo, out shenzhenFeeMasterId))
                {
                    taxEntity.ShenzhenFeeMasterId = shenzhenFeeMasterId;
                }
            }
        }

        /// <summary>
        /// 建立找不到託運單資料時的轉檔異常列。
        /// </summary>
        private static SeaShenzhenTaxTransferExceptionRow CreateExceptionRow(SeaShenzhenTaxEntity entity, string reason)
        {
            return new SeaShenzhenTaxTransferExceptionRow
            {
                Reason = reason,
                TrackingNo = entity.TrackingNo,
                Cod = entity.Cod,
                Tax = entity.Tax,
                Fee = entity.Fee
            };
        }

        /// <summary>
        /// 讀取稅單 Excel 並轉成系統使用的列資料。
        /// </summary>
        private List<SeaShenzhenTaxUploadRow> ReadExcelFile(string filePath, SeaShenzhenTaxUploadBrokerHeaderDefinition definition)
        {
            var result = new List<SeaShenzhenTaxUploadRow>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                var sheet = workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    return result;
                }

                int headerRowIndex;
                var headerMap = GetHeaderMap(sheet, definition, out headerRowIndex);

                for (var i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    var model = new SeaShenzhenTaxUploadRow
                    {
                        RowNo = i + 1,
                        TrackingNo = GetCellValue(row, headerMap, definition.TrackingNoHeaders),
                        CodText = GetCellValue(row, headerMap, definition.CodHeaders),
                        TaxText = GetCellValue(row, headerMap, definition.TaxHeaders),
                        FeeText = GetCellValue(row, headerMap, definition.FeeHeaders),
                        UploadStatus = "成功",
                        FailFieldName = string.Empty,
                        FailReason = string.Empty
                    };

                    if (IsEmptyRow(model) || !HasRequiredTransferKeys(model))
                    {
                        continue;
                    }

                    model.Cod = ParseNullableAmount(model.CodText);
                    model.Tax = ParseNullableAmount(model.TaxText);
                    model.Fee = ParseNullableAmount(model.FeeText);
                    result.Add(model);
                }
            }

            return result;
        }

        /// <summary>
        /// 依報關行欄位定義尋找 Excel 標題列與欄位索引。
        /// </summary>
        private static Dictionary<string, int> GetHeaderMap(ISheet sheet, SeaShenzhenTaxUploadBrokerHeaderDefinition definition, out int headerRowIndex)
        {
            for (var i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                {
                    continue;
                }

                var headerMap = new Dictionary<string, int>();
                for (var columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                {
                    var header = row.GetCellData(columnIndex);
                    if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header))
                    {
                        headerMap.Add(header.Trim(), columnIndex);
                    }
                }

                if (definition.RequiredHeaderGroups.All(group => group.Any(headerMap.ContainsKey)))
                {
                    headerRowIndex = i;
                    return headerMap;
                }
            }

            throw new Exception($"Excel 欄位格式不正確，{definition.DisplayName} 需包含欄位：{string.Join("、", definition.RequiredHeaderGroups.Select(x => x[0]))}");
        }

        /// <summary>
        /// 驗證上傳資料格式與必要欄位，並回寫失敗原因。
        /// </summary>
        private void ValidateUploadRows(List<SeaShenzhenTaxUploadRow> uploadRows, SeaShenzhenTaxUploadBrokerHeaderDefinition definition)
        {
            foreach (var item in uploadRows)
            {
                if (string.IsNullOrWhiteSpace(item.TrackingNo))
                {
                    AddValidationError(item, definition.TrackingNoHeaders[0], "必填");
                }

                if (string.IsNullOrWhiteSpace(item.TaxText))
                {
                    AddValidationError(item, definition.TaxHeaders[0], "必填");
                }
                else if (!item.Tax.HasValue)
                {
                    AddValidationError(item, definition.TaxHeaders[0], "格式錯誤");
                }

                if (!string.IsNullOrWhiteSpace(item.CodText) && !item.Cod.HasValue)
                {
                    AddValidationError(item, definition.CodHeaders[0], "格式錯誤");
                }

                if (!string.IsNullOrWhiteSpace(item.FeeText) && !item.Fee.HasValue)
                {
                    AddValidationError(item, definition.FeeHeaders[0], "格式錯誤");
                }
            }

            foreach (var item in uploadRows.Where(x => x.UploadStatus != "失敗"))
            {
                item.UploadStatus = "成功";
            }
        }

        /// <summary>
        /// 依欄位名稱集合取得 Excel 儲存格文字。
        /// </summary>
        private static string GetCellValue(IRow row, Dictionary<string, int> headerMap, IEnumerable<string> headerNames)
        {
            foreach (var headerName in headerNames)
            {
                int columnIndex;
                if (headerMap.TryGetValue(headerName, out columnIndex))
                {
                    return row.GetCellData(columnIndex).Trim();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 判斷列資料是否為空白列。
        /// </summary>
        private static bool IsEmptyRow(SeaShenzhenTaxUploadRow item)
        {
            return string.IsNullOrWhiteSpace(item.TrackingNo)
                && string.IsNullOrWhiteSpace(item.CodText)
                && string.IsNullOrWhiteSpace(item.TaxText)
                && string.IsNullOrWhiteSpace(item.FeeText);
        }

        /// <summary>
        /// 判斷列資料是否具備轉入必要鍵值。
        /// </summary>
        private static bool HasRequiredTransferKeys(SeaShenzhenTaxUploadRow item)
        {
            return !string.IsNullOrWhiteSpace(item.TrackingNo);
        }

        /// <summary>
        /// 將金額文字轉成整數金額。
        /// </summary>
        private static int? ParseNullableAmount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalizedValue = value.Trim().Replace(",", string.Empty);
            decimal amount;
            if (decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out amount)
                || decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.GetCultureInfo("zh-TW"), out amount))
            {
                return Convert.ToInt32(Math.Round(amount, MidpointRounding.AwayFromZero));
            }

            return null;
        }

        /// <summary>
        /// 將欄位驗證失敗原因追加到指定列資料。
        /// </summary>
        private static void AddValidationError(SeaShenzhenTaxUploadRow item, string fieldName, string reason)
        {
            item.UploadStatus = "失敗";

            var fieldNames = string.IsNullOrWhiteSpace(item.FailFieldName)
                ? new List<string>()
                : item.FailFieldName.Split(new[] { '、' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!fieldNames.Contains(fieldName))
            {
                fieldNames.Add(fieldName);
            }

            item.FailFieldName = string.Join("、", fieldNames);

            if (!string.IsNullOrWhiteSpace(item.FailReason))
            {
                item.FailReason += "；";
            }

            item.FailReason += $"{fieldName}：{reason}";
        }

        /// <summary>
        /// 建立轉檔異常明細 Excel。
        /// </summary>
        private static IWorkbook CreateTransferExceptionWorkbook(IEnumerable<SeaShenzhenTaxTransferExceptionRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("異常明細");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var headers = new[]
            {
                "原因",
                "託運單號(條碼號)",
                "到付金額",
                "稅金金額",
                "稅金手續費"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            var rowIndex = 1;
            foreach (var item in rows)
            {
                var row = sheet.CreateRow(rowIndex++);
                NpoiCell.CreateCell(row, 0, item.Reason, dataStyle);
                NpoiCell.CreateCell(row, 1, item.TrackingNo, dataStyle);
                NpoiCell.CreateIntCell(row, 2, item.Cod, dataStyle);
                NpoiCell.CreateIntCell(row, 3, item.Tax, dataStyle);
                NpoiCell.CreateIntCell(row, 4, item.Fee, dataStyle);
            }

            for (var index = 0; index < headers.Length; index++)
            {
                sheet.AutoSizeColumn(index);
                if (sheet.GetColumnWidth(index) < 3000)
                {
                    sheet.SetColumnWidth(index, 3000);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 依報關行取得對應的 Excel 欄位定義。
        /// </summary>
        private static SeaShenzhenTaxUploadBrokerHeaderDefinition GetHeaderDefinition(SeaShenzhenTaxDataType dataType)
        {
            switch (dataType)
            {
                case SeaShenzhenTaxDataType.Jetf:
                case SeaShenzhenTaxDataType.Shenzhen:
                    return new SeaShenzhenTaxUploadBrokerHeaderDefinition
                    {
                        DisplayName = dataType.ToDescription(),
                        TrackingNoHeaders = new[] { "託運單號(條碼號)" },
                        CodHeaders = new[] { "到付金额", "到付金額" },
                        TaxHeaders = new[] { "税金金额", "稅金金額" },
                        FeeHeaders = new[] { "税金手续费", "稅金手續費" }
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "不支援的報關行");
            }
        }
    }
}
