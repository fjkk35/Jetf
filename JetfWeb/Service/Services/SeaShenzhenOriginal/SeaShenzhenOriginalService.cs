using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    public class SeaShenzhenOriginalService : _BaseService
    {
        private static readonly string[] RequiredHeaders =
        {
            "報關號碼",
            "提單號碼",
            "訂單編號",
            "託運單號(條碼號)",
            "廠商交易時間",
            "寄件通路",
            "*收件人姓名",
            "收件門市代碼/地址(含配送備註)",
            "*收件人手機/電話",
            "商品名稱",
            "*代收金額",
            "數量",
            "重量",
            "備註",
            "認領人",
            "稅金支付方式"
        };

        public SeaShenzhenOriginalService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依條件查詢新遞託運資料。
        /// </summary>
        public SeaShenzhenOriginalQueryResponse GetData(SeaShenzhenOriginalQueryRequest request)
        {
            request = request ?? new SeaShenzhenOriginalQueryRequest();

            var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;
            pageSize = Math.Min(pageSize, 200);

            var startDate = ParseDateOnly(request.DataDateStart);
            var endDate = ParseDateOnly(request.DataDateEnd);
            var trackingNo = NullIfEmpty(request.TrackingNo);
            var blNo = NullIfEmpty(request.BlNo);
            var orderNo = NullIfEmpty(request.OrderNo);
            var jetfSerial = NullIfEmpty(request.JetfSerial);
            var importer = NullIfEmpty(request.Importer);
            var importerPhone = NullIfEmpty(request.ImporterPhone);

            var query = JetfDb.SeaShenzhenOriginals.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(x => x.DataDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endDateExclusive = endDate.Value.AddDays(1);
                query = query.Where(x => x.DataDate < endDateExclusive);
            }

            if (!string.IsNullOrWhiteSpace(trackingNo))
            {
                query = query.Where(x => x.TrackingNo.Contains(trackingNo));
            }

            if (!string.IsNullOrWhiteSpace(blNo))
            {
                query = query.Where(x => x.BlNo.Contains(blNo));
            }

            if (!string.IsNullOrWhiteSpace(orderNo))
            {
                query = query.Where(x => x.OrderNo.Contains(orderNo));
            }

            if (!string.IsNullOrWhiteSpace(jetfSerial))
            {
                query = query.Where(x => x.JetfSerial.Contains(jetfSerial));
            }

            if (!string.IsNullOrWhiteSpace(importer))
            {
                query = query.Where(x => x.Importer.Contains(importer));
            }

            if (!string.IsNullOrWhiteSpace(importerPhone))
            {
                query = query.Where(x => x.ImporterPhone.Contains(importerPhone));
            }

            var totalCount = query.Count();
            var data = query
                .OrderByDescending(x => x.DataDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(x => new SeaShenzhenOriginalQueryRow
                {
                    Id = x.Id,
                    DataDateText = x.DataDate.ToString("yyyy-MM-dd"),
                    TrackingNo = x.TrackingNo,
                    BlNo = x.BlNo,
                    OrderNo = x.OrderNo,
                    JetfSerial = x.JetfSerial,
                    TransTimeText = x.TransTime.HasValue ? x.TransTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                    TransName = x.TransName,
                    Importer = x.Importer,
                    ImporterAddress = x.ImporterAddress,
                    ImporterPhone = x.ImporterPhone,
                    ItemName = x.ItemName,
                    CcText = x.Cc.HasValue ? x.Cc.Value.ToString("0.##") : string.Empty,
                    QuantityText = x.Quantity.HasValue ? x.Quantity.Value.ToString() : string.Empty,
                    GwText = x.Gw.HasValue ? x.Gw.Value.ToString("0.##") : string.Empty,
                    Memo = x.Memo,
                    Claimant = x.Claimant,
                    TaxPayment = x.TaxPayment
                })
                .ToList();

            return new SeaShenzhenOriginalQueryResponse
            {
                TotalCount = totalCount,
                Data = data
            };
        }

        /// <summary>
        /// 上傳新遞託運資料。
        /// </summary>
        public ResponseModel Upload(string filePath, DateTime dataDate)
        {
            try
            {
                var uploadRows = ReadExcelFile(filePath);
                if (!uploadRows.Any())
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
                }

                ValidateUploadRows(uploadRows);

                var failList = uploadRows.Where(x => x.UploadStatus == "失敗").ToList();
                if (failList.Any())
                {
                    var failMessage = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，請修正後重新上傳";
                    return new ResponseModel
                    {
                        status = Status.error,
                        msg = failMessage,
                        ReturnObject = new
                        {
                            count = 0,
                            failCount = failList.Count,
                            data = failList,
                            message = failMessage
                        }
                    };
                }

                var saveResult = SaveUploadRows(uploadRows, dataDate);
                var successMessage = $"成功上傳 {uploadRows.Count} 筆資料，新增 {saveResult.InsertCount} 筆，修改 {saveResult.UpdateCount} 筆";
                return new ResponseModel
                {
                    status = Status.success,
                    msg = successMessage,
                    ReturnObject = new
                    {
                        count = uploadRows.Count,
                        insertCount = saveResult.InsertCount,
                        updateCount = saveResult.UpdateCount,
                        failCount = 0,
                        data = new List<SeaShenzhenOriginalUploadRow>(),
                        message = successMessage
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        private List<SeaShenzhenOriginalUploadRow> ReadExcelFile(string filePath)
        {
            var result = new List<SeaShenzhenOriginalUploadRow>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                var sheet = workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    return result;
                }

                int headerRowIndex;
                var headerMap = GetHeaderMap(sheet, out headerRowIndex);

                for (var i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    var model = new SeaShenzhenOriginalUploadRow
                    {
                        RowNo = i + 1,
                        TrackingNo = GetCellValue(row, headerMap, "報關號碼"),
                        BlNo = GetCellValue(row, headerMap, "提單號碼"),
                        OrderNo = GetCellValue(row, headerMap, "訂單編號"),
                        JetfSerial = GetCellValue(row, headerMap, "託運單號(條碼號)"),
                        TransTimeText = GetCellValue(row, headerMap, "廠商交易時間"),
                        TransName = GetCellValue(row, headerMap, "寄件通路"),
                        Importer = GetCellValue(row, headerMap, "*收件人姓名"),
                        ImporterAddress = GetCellValue(row, headerMap, "收件門市代碼/地址(含配送備註)"),
                        ImporterPhone = GetCellValue(row, headerMap, "*收件人手機/電話"),
                        ItemName = GetCellValue(row, headerMap, "商品名稱"),
                        CcText = GetCellValue(row, headerMap, "*代收金額"),
                        QuantityText = GetCellValue(row, headerMap, "數量"),
                        GwText = GetCellValue(row, headerMap, "重量"),
                        Memo = GetCellValue(row, headerMap, "備註"),
                        Claimant = GetCellValue(row, headerMap, "認領人"),
                        TaxPayment = GetCellValue(row, headerMap, "稅金支付方式"),
                        UploadStatus = "成功",
                        FailFieldName = string.Empty,
                        FailReason = string.Empty
                    };

                    if (IsEmptyRow(model))
                    {
                        continue;
                    }

                    model.TransTime = ParseNullableDate(row, headerMap["廠商交易時間"], model.TransTimeText);
                    model.Cc = ParseNullableDouble(model.CcText);
                    model.Quantity = ParseNullableInt(model.QuantityText);
                    model.Gw = ParseNullableDecimal(model.GwText);

                    result.Add(model);
                }
            }

            return result;
        }

        private Dictionary<string, int> GetHeaderMap(ISheet sheet, out int headerRowIndex)
        {
            for (var i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                {
                    continue;
                }

                var headerMap = new Dictionary<string, int>();
                for (var c = 0; c < row.LastCellNum; c++)
                {
                    var header = row.GetCellData(c);
                    if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header))
                    {
                        headerMap.Add(header, c);
                    }
                }

                if (RequiredHeaders.All(headerMap.ContainsKey))
                {
                    headerRowIndex = i;
                    return headerMap;
                }
            }

            throw new Exception($"Excel 欄位格式不正確，需包含欄位：{string.Join("、", RequiredHeaders)}");
        }

        private void ValidateUploadRows(List<SeaShenzhenOriginalUploadRow> uploadRows)
        {
            foreach (var item in uploadRows)
            {
                if (string.IsNullOrWhiteSpace(item.JetfSerial))
                {
                    AddValidationError(item, "託運單號(條碼號)", "必填");
                }

                if (string.IsNullOrWhiteSpace(item.Importer))
                {
                    AddValidationError(item, "*收件人姓名", "必填");
                }

                if (string.IsNullOrWhiteSpace(item.ImporterPhone))
                {
                    AddValidationError(item, "*收件人手機/電話", "必填");
                }

                if (string.IsNullOrWhiteSpace(item.CcText))
                {
                    AddValidationError(item, "*代收金額", "必填");
                }

                if (!string.IsNullOrWhiteSpace(item.TransTimeText) && !item.TransTime.HasValue)
                {
                    AddValidationError(item, "廠商交易時間", "格式錯誤");
                }

                if (!string.IsNullOrWhiteSpace(item.CcText) && !item.Cc.HasValue)
                {
                    AddValidationError(item, "*代收金額", "格式錯誤");
                }

                if (!string.IsNullOrWhiteSpace(item.QuantityText) && !item.Quantity.HasValue)
                {
                    AddValidationError(item, "數量", "格式錯誤");
                }

                if (!string.IsNullOrWhiteSpace(item.GwText) && !item.Gw.HasValue)
                {
                    AddValidationError(item, "重量", "格式錯誤");
                }
            }

            var duplicateJetfSerials = uploadRows
                .Where(x => !string.IsNullOrWhiteSpace(x.JetfSerial))
                .GroupBy(x => x.JetfSerial.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in uploadRows.Where(x => !string.IsNullOrWhiteSpace(x.JetfSerial) && duplicateJetfSerials.Contains(x.JetfSerial.Trim())))
            {
                AddValidationError(item, "託運單號(條碼號)", "Excel 內資料重複");
            }

            foreach (var item in uploadRows.Where(x => x.UploadStatus != "失敗"))
            {
                item.UploadStatus = "成功";
            }
        }

        private SaveResult SaveUploadRows(List<SeaShenzhenOriginalUploadRow> uploadRows, DateTime dataDate)
        {
            var now = DateTime.Now;
            var userId = GetUserId();
            var jetfSerials = uploadRows
                .Select(x => x.JetfSerial.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingEntities = JetfDb.SeaShenzhenOriginals
                .Where(x => jetfSerials.Contains(x.JetfSerial))
                .ToList();

            var existingMap = existingEntities
                .GroupBy(x => x.JetfSerial, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var insertCount = 0;
            var updateCount = 0;

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    foreach (var row in uploadRows)
                    {
                        var key = row.JetfSerial.Trim();
                        SeaShenzhenOriginalEntity entity;
                        if (existingMap.TryGetValue(key, out entity))
                        {
                            ApplyRow(entity, row, dataDate);
                            entity.ModifiedUser = userId;
                            entity.ModifiedTime = now;
                            updateCount++;
                        }
                        else
                        {
                            entity = new SeaShenzhenOriginalEntity
                            {
                                JetfSerial = key,
                                CreatedUser = userId,
                                CreatedTime = now
                            };
                            ApplyRow(entity, row, dataDate);
                            JetfDb.SeaShenzhenOriginals.Add(entity);
                            insertCount++;
                        }
                    }

                    JetfDb.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return new SaveResult
            {
                InsertCount = insertCount,
                UpdateCount = updateCount
            };
        }

        private void ApplyRow(SeaShenzhenOriginalEntity entity, SeaShenzhenOriginalUploadRow row, DateTime dataDate)
        {
            entity.DataDate = dataDate.Date;
            entity.TrackingNo = NullIfEmpty(row.TrackingNo);
            entity.BlNo = NullIfEmpty(row.BlNo);
            entity.OrderNo = NullIfEmpty(row.OrderNo);
            entity.JetfSerial = row.JetfSerial.Trim();
            entity.TransTime = row.TransTime;
            entity.TransName = NullIfEmpty(row.TransName);
            entity.Importer = NullIfEmpty(row.Importer);
            entity.ImporterAddress = NullIfEmpty(row.ImporterAddress);
            entity.ImporterPhone = NullIfEmpty(row.ImporterPhone);
            entity.ItemName = NullIfEmpty(row.ItemName);
            entity.Cc = row.Cc;
            entity.Quantity = row.Quantity;
            entity.Gw = row.Gw;
            entity.Memo = NullIfEmpty(row.Memo);
            entity.Claimant = NullIfEmpty(row.Claimant);
            entity.TaxPayment = NullIfEmpty(row.TaxPayment);
        }

        private void AddValidationError(SeaShenzhenOriginalUploadRow item, string fieldName, string reason)
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

        private string GetCellValue(IRow row, Dictionary<string, int> headerMap, string headerName)
        {
            return headerMap.ContainsKey(headerName)
                ? row.GetCellData(headerMap[headerName]).Trim()
                : string.Empty;
        }

        private bool IsEmptyRow(SeaShenzhenOriginalUploadRow item)
        {
            return string.IsNullOrWhiteSpace(item.TrackingNo)
                && string.IsNullOrWhiteSpace(item.BlNo)
                && string.IsNullOrWhiteSpace(item.OrderNo)
                && string.IsNullOrWhiteSpace(item.JetfSerial)
                && string.IsNullOrWhiteSpace(item.TransTimeText)
                && string.IsNullOrWhiteSpace(item.TransName)
                && string.IsNullOrWhiteSpace(item.Importer)
                && string.IsNullOrWhiteSpace(item.ImporterAddress)
                && string.IsNullOrWhiteSpace(item.ImporterPhone)
                && string.IsNullOrWhiteSpace(item.ItemName)
                && string.IsNullOrWhiteSpace(item.CcText)
                && string.IsNullOrWhiteSpace(item.QuantityText)
                && string.IsNullOrWhiteSpace(item.GwText)
                && string.IsNullOrWhiteSpace(item.Memo)
                && string.IsNullOrWhiteSpace(item.Claimant)
                && string.IsNullOrWhiteSpace(item.TaxPayment);
        }

        private DateTime? ParseNullableDate(IRow row, int cellIndex, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var cell = row.GetCell(cellIndex);
            if (cell != null && cell.CellType == CellType.Numeric)
            {
                if (DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue;
                }

                return DateTime.FromOADate(cell.NumericCellValue);
            }

            DateTime dateValue;
            if (DateTime.TryParse(text, out dateValue))
            {
                return dateValue;
            }

            return null;
        }

        private int? ParseNullableInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            int intValue;
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out intValue)
                || int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
            {
                return intValue;
            }

            decimal decimalValue;
            if ((decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue)
                || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
                && decimal.Truncate(decimalValue) == decimalValue)
            {
                return Convert.ToInt32(decimalValue);
            }

            return null;
        }

        private decimal? ParseNullableDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            decimal decimalValue;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue)
                || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
            {
                return decimalValue;
            }

            return null;
        }

        private double? ParseNullableDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            double doubleValue;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out doubleValue)
                || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
            {
                return doubleValue;
            }

            return null;
        }

        private string NullIfEmpty(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private DateTime? ParseDateOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            DateTime dateValue;
            if (DateTime.TryParse(text, out dateValue))
            {
                return dateValue.Date;
            }

            return null;
        }

        private class SaveResult
        {
            public int InsertCount { get; set; }
            public int UpdateCount { get; set; }
        }
    }
}
