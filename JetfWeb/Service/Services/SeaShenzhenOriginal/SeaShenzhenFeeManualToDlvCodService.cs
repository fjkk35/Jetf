using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞深圳金額人工調整服務。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodService : _BaseService
    {
        private const string DlvInvHeader = "託運單號(條碼號)";

        private const string CodHeader = "到付运费金额";

        private const string TaxHeader = "税金金额";

        private const string FeeHeader = "税金手续费";

        private static readonly string[] RequiredHeaders =
        {
            DlvInvHeader,
            CodHeader,
            TaxHeader,
            FeeHeader
        };

        public SeaShenzhenFeeManualToDlvCodService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳金額人工調整資料。
        /// </summary>
        public ResponseModel Upload(string filePath)
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

                var adjustmentCount = SaveUploadRows(uploadRows);
                var successMessage = $"成功上傳 {uploadRows.Count} 筆資料，新增 {adjustmentCount} 筆人工調整紀錄";

                return new ResponseModel
                {
                    status = Status.success,
                    msg = successMessage,
                    ReturnObject = new
                    {
                        count = uploadRows.Count,
                        insertCount = adjustmentCount,
                        updateCount = 0,
                        failCount = 0,
                        data = new List<SeaShenzhenFeeManualToDlvCodUploadRow>(),
                        message = successMessage
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 下載範例檔案。
        /// </summary>
        public byte[] ExportTemplate()
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("金額人工調整範例");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, RequiredHeaders, headerStyle);

            var dataRow = sheet.CreateRow(1);
            NpoiCell.CreateCell(dataRow, 0, "SF123456789", dataStyle);
            NpoiCell.CreateIntCell(dataRow, 1, 100, dataStyle);
            NpoiCell.CreateIntCell(dataRow, 2, 50, dataStyle);
            NpoiCell.CreateIntCell(dataRow, 3, 10, dataStyle);

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
        /// 依條件查詢人工調整資料，依建立時間由新到舊排序。
        /// </summary>
        public SeaShenzhenFeeManualToDlvCodQueryResponse GetData(SeaShenzhenFeeManualToDlvCodQueryRequest request)
        {
            request = request ?? new SeaShenzhenFeeManualToDlvCodQueryRequest();

            var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;
            pageSize = Math.Min(pageSize, 200);

            var dlvInv = NullIfEmpty(request.DlvInv);

            var query = JetfDb.ShenzhenFeeMasterManualToDlvCods.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(dlvInv))
            {
                query = query.Where(x => x.DlvInv.Contains(dlvInv));
            }

            var totalCount = query.Count();
            var data = query
                .OrderByDescending(x => x.CreatedTime)
                .ThenByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(x => new SeaShenzhenFeeManualToDlvCodQueryRow
                {
                    Id = x.Id,
                    DlvInv = x.DlvInv,
                    Cod = x.Cod,
                    Tax = x.Tax,
                    Fee = x.Fee,
                    CreatedTimeText = x.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedUser = x.CreatedUser
                })
                .ToList();

            return new SeaShenzhenFeeManualToDlvCodQueryResponse
            {
                TotalCount = totalCount,
                Data = data
            };
        }

        /// <summary>
        /// 讀取人工調整 Excel 並轉成列資料。
        /// </summary>
        private List<SeaShenzhenFeeManualToDlvCodUploadRow> ReadExcelFile(string filePath)
        {
            var result = new List<SeaShenzhenFeeManualToDlvCodUploadRow>();

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

                    var model = new SeaShenzhenFeeManualToDlvCodUploadRow
                    {
                        RowNo = i + 1,
                        DlvInv = GetCellValue(row, headerMap, DlvInvHeader),
                        CodText = GetCellValue(row, headerMap, CodHeader),
                        TaxText = GetCellValue(row, headerMap, TaxHeader),
                        FeeText = GetCellValue(row, headerMap, FeeHeader),
                        UploadStatus = "成功",
                        FailFieldName = string.Empty,
                        FailReason = string.Empty
                    };

                    if (string.IsNullOrWhiteSpace(model.DlvInv) && AreAmountsBlank(model))
                    {
                        continue;
                    }

                    model.Cod = ParseAmount(model.CodText);
                    model.Tax = ParseAmount(model.TaxText);
                    model.Fee = ParseAmount(model.FeeText);
                    result.Add(model);
                }
            }

            return result;
        }

        /// <summary>
        /// 建立人工調整上傳欄位對照表。
        /// </summary>
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
                    if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header.Trim()))
                    {
                        headerMap.Add(header.Trim(), c);
                    }
                }

                if (headerMap.ContainsKey(DlvInvHeader)
                    && headerMap.ContainsKey(CodHeader)
                    && headerMap.ContainsKey(TaxHeader)
                    && headerMap.ContainsKey(FeeHeader))
                {
                    headerRowIndex = i;
                    return headerMap;
                }
            }

            throw new Exception($"Excel 欄位格式不正確，需包含欄位：{string.Join("、", RequiredHeaders)}");
        }

        /// <summary>
        /// 驗證金額人工調整上傳資料，並確認可對應到既有託運資料。
        /// </summary>
        private void ValidateUploadRows(List<SeaShenzhenFeeManualToDlvCodUploadRow> uploadRows)
        {
            foreach (var item in uploadRows)
            {
                if (string.IsNullOrWhiteSpace(item.DlvInv))
                {
                    AddValidationError(item, DlvInvHeader, "必填");
                }

                if (AreAmountsBlank(item))
                {
                    AddValidationError(item, "金額欄位", "到付運費、稅金、稅金手續費至少須填一項");
                }

                if (!item.Cod.HasValue)
                {
                    AddValidationError(item, CodHeader, "格式錯誤");
                }

                if (!item.Tax.HasValue)
                {
                    AddValidationError(item, TaxHeader, "格式錯誤");
                }

                if (!item.Fee.HasValue)
                {
                    AddValidationError(item, FeeHeader, "格式錯誤");
                }
            }

            var dlvInvs = uploadRows
                .Where(x => x.UploadStatus != "失敗" && !string.IsNullOrWhiteSpace(x.DlvInv))
                .Select(x => x.DlvInv.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (dlvInvs.Count == 0)
            {
                return;
            }

            var originalDlvInvs = JetfDb.SeaShenzhenOriginals
                .Where(x => dlvInvs.Contains(x.JetfSerial))
                .Select(x => x.JetfSerial)
                .Distinct()
                .ToList();
            var originalKeys = new HashSet<string>(originalDlvInvs.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

            foreach (var item in uploadRows.Where(x => x.UploadStatus != "失敗"))
            {
                if (!originalKeys.Contains(item.DlvInv.Trim()))
                {
                    AddValidationError(item, DlvInvHeader, "找不到託運資料");
                }
            }
        }

        /// <summary>
        /// 同步更新稅金與託運主檔，並回傳本次新增的人工調整紀錄筆數。
        /// </summary>
        private int SaveUploadRows(List<SeaShenzhenFeeManualToDlvCodUploadRow> uploadRows)
        {
            var now = DateTime.Now;
            var userId = GetUserId();

            // 同一託運單號在檔案中出現多次時，以最後一列的金額為準。
            var rows = uploadRows
                .GroupBy(x => x.DlvInv.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Last())
                .ToList();
            var dlvInvs = rows
                .Select(x => x.DlvInv.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 在同一交易內更新主檔及寫入歷程，避免同時匯入同一單號時重複建立稅金主檔。
            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var originalDic = JetfDb.SeaShenzhenOriginals
                    .Where(x => dlvInvs.Contains(x.JetfSerial))
                    .OrderBy(x => x.Id)
                    .AsEnumerable()
                    .GroupBy(x => x.JetfSerial.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
                var feeMasterDic = JetfDb.ShenzhenFeeMasters
                    .Where(x => dlvInvs.Contains(x.DlvInv))
                    .AsEnumerable()
                    .GroupBy(x => x.DlvInv.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    var dlvInv = row.DlvInv.Trim();
                    if (!originalDic.TryGetValue(dlvInv, out var originals))
                    {
                        throw new InvalidOperationException($"{dlvInv}：找不到託運資料");
                    }

                    // 取 Id 最小的託運資料作為來源，並同步更新同單號所有託運資料的到付運費。
                    var source = originals[0];
                    foreach (var original in originals)
                    {
                        original.Cc = row.Cod.Value;
                        original.ModifiedUser = userId;
                        original.ModifiedTime = now;
                    }

                    // 同單號的每筆稅金主檔都套用匯入金額，並依各自的稅金支付方式重算代收金額。
                    if (feeMasterDic.TryGetValue(dlvInv, out var feeMasters))
                    {
                        foreach (var feeMaster in feeMasters)
                        {
                            feeMaster.Cod = row.Cod.Value;
                            feeMaster.Tax = row.Tax.Value;
                            feeMaster.Fee = row.Fee.Value;
                            feeMaster.ToDlvCod = SeaShenzhenFeeTransferShared.CalculateToDlvCod(
                                feeMaster.IncludeTax, feeMaster.Tax, feeMaster.Cod, feeMaster.Fee);
                            feeMaster.ModifiedUser = userId;
                            feeMaster.ModifiedTime = now;
                        }
                    }
                    else
                    {
                        // 尚無稅金主檔時，用託運資料及剛更新的到付運費建立一筆。
                        var feeMaster = SeaShenzhenFeeTransferShared.CreateTransferRow(
                            source,
                            row.Tax.Value,
                            source.DataDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                            customer: null,
                            dataType: source.DataType,
                            feeMasterId: null,
                            userId: userId,
                            now: now,
                            feeOverride: row.Fee.Value);
                        JetfDb.ShenzhenFeeMasters.Add(feeMaster);
                    }

                    // 每次套用調整都新增一筆歷程，保留當次的金額、操作人員與時間。
                    JetfDb.ShenzhenFeeMasterManualToDlvCods.Add(new ShenzhenFeeMasterManualToDlvCodEntity
                    {
                        TrackingNo = source.TrackingNo,
                        DlvInv = dlvInv,
                        Cod = row.Cod.Value,
                        Tax = row.Tax.Value,
                        Fee = row.Fee.Value,
                        CreatedUser = userId,
                        CreatedTime = now
                    });
                }

                JetfDb.SaveChanges();
                transaction.Commit();
            }

            return rows.Count;
        }

        /// <summary>
        /// 依欄位名稱取得 Excel 儲存格文字。
        /// </summary>
        private static string GetCellValue(IRow row, Dictionary<string, int> headerMap, string headerName)
        {
            int columnIndex;
            if (headerMap.TryGetValue(headerName, out columnIndex))
            {
                return row.GetCellData(columnIndex).Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// 判斷三個金額欄位是否皆未填寫。
        /// </summary>
        private static bool AreAmountsBlank(SeaShenzhenFeeManualToDlvCodUploadRow item)
        {
            return string.IsNullOrWhiteSpace(item.CodText)
                && string.IsNullOrWhiteSpace(item.TaxText)
                && string.IsNullOrWhiteSpace(item.FeeText);
        }

        /// <summary>
        /// 將金額轉為整數，空白視為零，格式錯誤則回傳 null。
        /// </summary>
        private static int? ParseAmount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
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
                && decimal.Truncate(decimalValue) == decimalValue
                && decimalValue >= int.MinValue && decimalValue <= int.MaxValue)
            {
                return Convert.ToInt32(decimalValue);
            }

            return null;
        }

        /// <summary>
        /// 將空白字串正規化為 null。
        /// </summary>
        private static string NullIfEmpty(string text)
        {
            var trimmedText = (text ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(trimmedText) ? null : trimmedText;
        }

        /// <summary>
        /// 將欄位驗證失敗原因追加到指定列資料。
        /// </summary>
        private static void AddValidationError(SeaShenzhenFeeManualToDlvCodUploadRow item, string fieldName, string reason)
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

    }
}
