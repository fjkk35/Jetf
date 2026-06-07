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
    /// <summary>
    /// 新遞深圳物流代收金額人工調整服務。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodService : _BaseService
    {
        private static readonly string[] RequiredHeaders =
        {
            "物流貨號",
            "代收金額"
        };

        public SeaShenzhenFeeManualToDlvCodService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳代收金額人工調整資料。
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

                var saveResult = SaveUploadRows(uploadRows);
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
            var sheet = workbook.CreateSheet("代收金額人工調整範例");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, RequiredHeaders, headerStyle);

            var dataRow = sheet.CreateRow(1);
            NpoiCell.CreateCell(dataRow, 0, "SF123456789", dataStyle);
            NpoiCell.CreateIntCell(dataRow, 1, 100, dataStyle);

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
                        DlvInv = GetCellValue(row, headerMap, "物流貨號"),
                        ToDlvCodText = GetCellValue(row, headerMap, "代收金額"),
                        UploadStatus = "成功",
                        FailFieldName = string.Empty,
                        FailReason = string.Empty
                    };

                    if (IsEmptyRow(model))
                    {
                        continue;
                    }

                    model.ToDlvCod = ParseNullableInt(model.ToDlvCodText);
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

        /// <summary>
        /// 驗證人工調整上傳資料，並確認可對應到既有託運資料。
        /// </summary>
        private void ValidateUploadRows(List<SeaShenzhenFeeManualToDlvCodUploadRow> uploadRows)
        {
            foreach (var item in uploadRows)
            {
                if (string.IsNullOrWhiteSpace(item.DlvInv))
                {
                    AddValidationError(item, "物流貨號", "必填");
                }

                if (string.IsNullOrWhiteSpace(item.ToDlvCodText))
                {
                    AddValidationError(item, "代收金額", "必填");
                }
                else if (!item.ToDlvCod.HasValue)
                {
                    AddValidationError(item, "代收金額", "格式錯誤");
                }
                else if (item.ToDlvCod.Value <= 0)
                {
                    AddValidationError(item, "代收金額", "必須大於 0");
                }
            }

            var dlvInvs = uploadRows
                .Where(x => !string.IsNullOrWhiteSpace(x.DlvInv))
                .Select(x => x.DlvInv.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (dlvInvs.Count == 0)
            {
                return;
            }

            var originalLookup = JetfDb.SeaShenzhenOriginals
                .AsNoTracking()
                .Where(x => dlvInvs.Contains(x.JetfSerial))
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x.JetfSerial))
                .GroupBy(x => x.JetfSerial, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(x => x.Id).First(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var item in uploadRows)
            {
                if (item.UploadStatus == "失敗")
                {
                    continue;
                }

                var key = item.DlvInv.Trim();
                SeaShenzhenOriginalEntity original;
                if (!originalLookup.TryGetValue(key, out original))
                {
                    AddValidationError(item, "物流貨號", "找不到託運資料");
                    continue;
                }

                item.TrackingNo = original.TrackingNo;
            }

            foreach (var item in uploadRows.Where(x => x.UploadStatus != "失敗"))
            {
                item.UploadStatus = "成功";
            }
        }

        /// <summary>
        /// 將驗證成功的人工調整資料寫入資料表。
        /// </summary>
        private SaveResult SaveUploadRows(List<SeaShenzhenFeeManualToDlvCodUploadRow> uploadRows)
        {
            var now = DateTime.Now;
            var userId = GetUserId();
            var dlvInvs = uploadRows
                .Select(x => x.DlvInv.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingEntities = JetfDb.ShenzhenFeeMasterManualToDlvCods
                .Where(x => dlvInvs.Contains(x.DlvInv))
                .ToList();

            var existingMap = existingEntities
                .GroupBy(x => x.DlvInv, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var insertCount = 0;
            var updateCount = 0;

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    foreach (var row in uploadRows)
                    {
                        var key = row.DlvInv.Trim();
                        ShenzhenFeeMasterManualToDlvCodEntity entity;
                        if (existingMap.TryGetValue(key, out entity))
                        {
                            entity.TrackingNo = row.TrackingNo;
                            entity.DlvInv = key;
                            entity.ToDlvCod = row.ToDlvCod ?? 0;
                            entity.ModifiedUser = userId;
                            entity.ModifiedTime = now;
                            updateCount++;
                        }
                        else
                        {
                            entity = new ShenzhenFeeMasterManualToDlvCodEntity
                            {
                                TrackingNo = row.TrackingNo,
                                DlvInv = key,
                                ToDlvCod = row.ToDlvCod ?? 0,
                                CreatedUser = userId,
                                CreatedTime = now,
                                ModifiedUser = userId,
                                ModifiedTime = now
                            };
                            JetfDb.ShenzhenFeeMasterManualToDlvCods.Add(entity);
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

        /// <summary>
        /// 依欄位名稱取得 Excel 儲存格文字。
        /// </summary>
        private static string GetCellValue(IRow row, Dictionary<string, int> headerMap, string headerName)
        {
            return headerMap.ContainsKey(headerName)
                ? row.GetCellData(headerMap[headerName]).Trim()
                : string.Empty;
        }

        /// <summary>
        /// 判斷 Excel 列是否為空白列。
        /// </summary>
        private static bool IsEmptyRow(SeaShenzhenFeeManualToDlvCodUploadRow item)
        {
            return string.IsNullOrWhiteSpace(item.DlvInv)
                && string.IsNullOrWhiteSpace(item.ToDlvCodText);
        }

        /// <summary>
        /// 將文字轉成 nullable int。
        /// </summary>
        private static int? ParseNullableInt(string text)
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

        /// <summary>
        /// 寫入結果統計。
        /// </summary>
        private class SaveResult
        {
            public int InsertCount { get; set; }

            public int UpdateCount { get; set; }
        }
    }
}
