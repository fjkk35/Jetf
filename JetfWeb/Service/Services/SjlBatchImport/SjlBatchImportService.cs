using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Services.SjlBatchImport.Domain;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SjlBatchImport
{
    public class SjlBatchImportService : _BaseService
    {
        private static readonly string[] RequiredHeaders =
        {
            "運送編號",
            "單據編號",
            "編號",
            "收件人",
            "派送日",
            "其他費用",
            "代收",
            "地址",
            "品名",
            "件數",
            "材積",
            "重量",
            "收件人電話"
        };

        /// <summary>
        /// 上傳捷利托運資料。
        /// </summary>
        public ResopnseModel Upload(string filePath)
        {
            try
            {
                var uploadRows = ReadExcelFile(filePath);
                if (uploadRows.Count == 0)
                {
                    return new ResopnseModel("Excel 檔案中沒有資料");
                }

                ValidateUploadRows(uploadRows);

                var failList = uploadRows.Where(x => x.UploadStatus == "失敗").ToList();
                if (failList.Any())
                {
                    var failMessage = $"上傳失敗，共 {failList.Count} 筆資料有錯誤，請修正後重新上傳";
                    return new ResopnseModel
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

                SaveUploadRows(uploadRows);

                var successMessage = $"成功上傳 {uploadRows.Count} 筆資料";
                return new ResopnseModel
                {
                    status = Status.success,
                    msg = successMessage,
                    ReturnObject = new
                    {
                        count = uploadRows.Count,
                        failCount = 0,
                        data = new List<SjlShippingDataUploadModel>(),
                        message = successMessage
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResopnseModel($"上傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢捷利托運資料。
        /// </summary>
        public SjlBatchImportSearchResponse GetSearchData(SjlBatchImportSearchRequest request)
        {
            var parameters = BuildSearchParameters(request);
            const string sql = @"
SELECT COUNT(1)
FROM [jetf].[dbo].[SjlShippingData]
WHERE (@StartDate IS NULL OR [CreatedTime] >= @StartDate)
  AND (@EndDate IS NULL OR [CreatedTime] < @EndDate)
    AND (@JetfSerial = '' OR [JetfSerial] = @JetfSerial);

SELECT
    [Id],
    [JetfSerial],
    [BagNumber],
    [Seq],
    [Importer],
    [DeliveryDate],
    [OtherFee],
    [Cod],
    [ImporterAddr],
    [ItemName],
    [Qty],
    [Volume],
    [Gw],
    [ImporterPhone],
    [TransName],
    [CreatedTime]
FROM [jetf].[dbo].[SjlShippingData]
WHERE (@StartDate IS NULL OR [CreatedTime] >= @StartDate)
  AND (@EndDate IS NULL OR [CreatedTime] < @EndDate)
    AND (@JetfSerial = '' OR [JetfSerial] = @JetfSerial)
ORDER BY [CreatedTime] DESC, [Id] DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using (var query = conn.QueryMultiple(sql, parameters))
            {
                return new SjlBatchImportSearchResponse
                {
                    TotalCount = query.ReadFirst<int>(),
                    Data = query.Read<SjlShippingDataSearchModel>().ToList()
                };
            }
        }

        /// <summary>
        /// 修改派件公司並寫入歷史資料。
        /// </summary>
        public ResopnseModel UpdateTransName(SjlShippingDataUpdateTransNameRequest request)
        {
            if (request == null)
            {
                return new ResopnseModel("資料不存在");
            }

            var targetIds = new List<int>();
            if (request.SjlShippingDataIds != null && request.SjlShippingDataIds.Any())
            {
                targetIds.AddRange(request.SjlShippingDataIds);
            }

            if (request.SjlShippingDataId > 0)
            {
                targetIds.Add(request.SjlShippingDataId);
            }

            targetIds = targetIds.Where(x => x > 0).Distinct().ToList();
            if (!targetIds.Any())
            {
                return new ResopnseModel("請至少選擇一筆資料");
            }

            var newTransName = NullIfEmpty(request.TransName);
            if (newTransName != "大榮" && newTransName != "捷通" && newTransName != "捷穩通")
            {
                return new ResopnseModel("派件公司僅能為大榮、捷通或捷穩通");
            }

            var userId = GetUserId();

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var currentData = connection.Query<SjlShippingDataSearchModel>(@"
SELECT [Id], [TransName]
FROM [jetf].[dbo].[SjlShippingData]
WHERE [Id] IN @Ids", new
                        {
                            Ids = targetIds
                        }, transaction).ToList();

                        if (!currentData.Any())
                        {
                            transaction.Rollback();
                            return new ResopnseModel("查無資料");
                        }

                        if (currentData.Count != targetIds.Count)
                        {
                            transaction.Rollback();
                            return new ResopnseModel("部分資料不存在，請重新查詢後再試");
                        }

                        var pendingData = currentData
                            .Where(x => !string.Equals(NullIfEmpty(x.TransName), newTransName, StringComparison.Ordinal))
                            .ToList();

                        if (!pendingData.Any())
                        {
                            transaction.Rollback();
                            return new ResopnseModel("派件公司未異動，不需修改");
                        }

                        var pendingIds = pendingData.Select(x => x.Id).ToList();

                        connection.Execute(@"
UPDATE [jetf].[dbo].[SjlShippingData]
SET [TransName] = @TransName,
    [UpdatedOpe] = @UpdatedOpe,
    [UpdatedTime] = GETDATE()
WHERE [Id] IN @Ids", new
                        {
                            Ids = pendingIds,
                            TransName = newTransName,
                            UpdatedOpe = userId
                        }, transaction);

                        connection.Execute(@"
INSERT INTO [jetf].[dbo].[SjlShippingDataTransNameHistory]
([SjlShippingDataId], [OldTransName], [NewTransName], [CreatedOpe], [CreatedTime])
VALUES
(@SjlShippingDataId, @OldTransName, @NewTransName, @CreatedOpe, GETDATE())", pendingData.Select(x => new
                        {
                            SjlShippingDataId = x.Id,
                            OldTransName = NullIfEmpty(x.TransName),
                            NewTransName = newTransName,
                            CreatedOpe = userId
                        }).ToList(), transaction);

                        transaction.Commit();

                        var skippedCount = targetIds.Count - pendingIds.Count;
                        var message = pendingIds.Count == 1 && skippedCount == 0
                            ? "修改成功"
                            : skippedCount > 0
                                ? $"成功修改 {pendingIds.Count} 筆，略過 {skippedCount} 筆未異動資料"
                                : $"成功修改 {pendingIds.Count} 筆資料";

                        return new ResopnseModel
                        {
                            status = Status.success,
                            msg = message,
                            ReturnObject = new
                            {
                                Id = pendingIds.Count == 1 ? pendingIds[0] : 0,
                                Ids = pendingIds,
                                TransName = newTransName,
                                UpdatedCount = pendingIds.Count,
                                SkippedCount = skippedCount
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new ResopnseModel($"修改失敗：{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 讀取 Excel 檔案。
        /// </summary>
        private List<SjlShippingDataUploadModel> ReadExcelFile(string filePath)
        {
            var result = new List<SjlShippingDataUploadModel>();

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

                for (int i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    var model = new SjlShippingDataUploadModel
                    {
                        RowNo = i + 1,
                        JetfSerial = GetCellValue(row, headerMap, "運送編號"),
                        BagNumber = GetCellValue(row, headerMap, "單據編號"),
                        Seq = GetCellValue(row, headerMap, "編號"),
                        Importer = GetCellValue(row, headerMap, "收件人"),
                        DeliveryDateText = GetCellValue(row, headerMap, "派送日"),
                        OtherFeeText = GetCellValue(row, headerMap, "其他費用"),
                        CodText = GetCellValue(row, headerMap, "代收"),
                        ImporterAddr = GetCellValue(row, headerMap, "地址"),
                        ItemName = GetCellValue(row, headerMap, "品名"),
                        QtyText = GetCellValue(row, headerMap, "件數"),
                        VolumeText = GetCellValue(row, headerMap, "材積"),
                        GwText = GetCellValue(row, headerMap, "重量"),
                        ImporterPhone = GetCellValue(row, headerMap, "收件人電話")
                    };

                    if (IsEmptyRow(model))
                    {
                        continue;
                    }

                    model.DeliveryDate = ParseNullableDate(row, headerMap["派送日"], model.DeliveryDateText);
                    model.OtherFee = ParseNullableDecimal(model.OtherFeeText);
                    model.Cod = ParseNullableDecimal(model.CodText);
                    model.Qty = ParseNullableInt(model.QtyText);
                    model.Volume = ParseNullableDecimal(model.VolumeText);
                    model.Gw = ParseNullableDecimal(model.GwText);
                    model.UploadStatus = "成功";
                    model.FailFieldName = string.Empty;
                    model.FailReason = string.Empty;

                    result.Add(model);
                }
            }

            return result;
        }

        /// <summary>
        /// 取得 Excel 表頭對應欄位。
        /// </summary>
        private Dictionary<string, int> GetHeaderMap(ISheet sheet, out int headerRowIndex)
        {
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                {
                    continue;
                }

                var headerMap = new Dictionary<string, int>();
                for (int c = 0; c < row.LastCellNum; c++)
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
        /// 驗證上傳資料。
        /// </summary>
        private void ValidateUploadRows(List<SjlShippingDataUploadModel> uploadRows)
        {
            foreach (var item in uploadRows)
            {
                if (string.IsNullOrWhiteSpace(item.JetfSerial))
                {
                    AddValidationError(item, "運送編號", "必填");
                }

                if (string.IsNullOrWhiteSpace(item.BagNumber))
                {
                    AddValidationError(item, "單據編號", "必填");
                }

                if (!string.IsNullOrWhiteSpace(item.VolumeText) && !item.Volume.HasValue)
                {
                    AddValidationError(item, "材積", "格式錯誤");
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
                AddValidationError(item, "運送編號", "Excel 內資料重複");
            }

            foreach (var item in uploadRows.Where(x => x.UploadStatus != "失敗"))
            {
                item.UploadStatus = "成功";
            }
        }

        /// <summary>
        /// 寫入捷利托運資料。
        /// </summary>
        private void SaveUploadRows(List<SjlShippingDataUploadModel> uploadRows)
        {
            var userId = GetUserId();
            var jetfSerials = uploadRows.Select(x => x.JetfSerial.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            const string selectSql = @"
SELECT [JetfSerial]
FROM [jetf].[dbo].[SjlShippingData]
WHERE [JetfSerial] IN @JetfSerials";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                connection.Open();

                var existingJetfSerials = connection.Query<string>(selectSql, new { JetfSerials = jetfSerials })
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var updateRows = uploadRows.Where(x => existingJetfSerials.Contains(x.JetfSerial.Trim())).ToList();
                var insertRows = uploadRows.Where(x => !existingJetfSerials.Contains(x.JetfSerial.Trim())).ToList();

                const string updateSql = @"
UPDATE [jetf].[dbo].[SjlShippingData]
SET [BagNumber] = @BagNumber,
    [Seq] = @Seq,
    [Importer] = @Importer,
    [DeliveryDate] = @DeliveryDate,
    [OtherFee] = @OtherFee,
    [Cod] = @Cod,
    [ImporterAddr] = @ImporterAddr,
    [ItemName] = @ItemName,
    [Qty] = @Qty,
    [Volume] = @Volume,
    [Gw] = @Gw,
    [ImporterPhone] = @ImporterPhone,
    [UpdatedOpe] = @UpdatedOpe,
    [UpdatedTime] = GETDATE()
WHERE [JetfSerial] = @JetfSerial";

                const string insertSql = @"
INSERT INTO [jetf].[dbo].[SjlShippingData]
([JetfSerial], [BagNumber], [Seq], [Importer], [DeliveryDate], [OtherFee], [Cod], [ImporterAddr], [ItemName], [Qty], [Volume], [Gw], [ImporterPhone], [CreatedOpe], [CreatedTime])
VALUES
(@JetfSerial, @BagNumber, @Seq, @Importer, @DeliveryDate, @OtherFee, @Cod, @ImporterAddr, @ItemName, @Qty, @Volume, @Gw, @ImporterPhone, @CreatedOpe, GETDATE())";

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (updateRows.Any())
                        {
                            connection.Execute(updateSql, updateRows.Select(x => new
                            {
                                JetfSerial = x.JetfSerial.Trim(),
                                BagNumber = NullIfEmpty(x.BagNumber),
                                Seq = NullIfEmpty(x.Seq),
                                Importer = NullIfEmpty(x.Importer),
                                DeliveryDate = x.DeliveryDate,
                                OtherFee = x.OtherFee,
                                Cod = x.Cod,
                                ImporterAddr = NullIfEmpty(x.ImporterAddr),
                                ItemName = NullIfEmpty(x.ItemName),
                                Qty = x.Qty,
                                Volume = x.Volume,
                                Gw = x.Gw,
                                ImporterPhone = NullIfEmpty(x.ImporterPhone),
                                UpdatedOpe = userId
                            }), transaction);
                        }

                        if (insertRows.Any())
                        {
                            connection.Execute(insertSql, insertRows.Select(x => new
                            {
                                JetfSerial = x.JetfSerial.Trim(),
                                BagNumber = NullIfEmpty(x.BagNumber),
                                Seq = NullIfEmpty(x.Seq),
                                Importer = NullIfEmpty(x.Importer),
                                DeliveryDate = x.DeliveryDate,
                                OtherFee = x.OtherFee,
                                Cod = x.Cod,
                                ImporterAddr = NullIfEmpty(x.ImporterAddr),
                                ItemName = NullIfEmpty(x.ItemName),
                                Qty = x.Qty,
                                Volume = x.Volume,
                                Gw = x.Gw,
                                ImporterPhone = NullIfEmpty(x.ImporterPhone),
                                CreatedOpe = userId
                            }), transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 加入驗證錯誤。
        /// </summary>
        private void AddValidationError(SjlShippingDataUploadModel item, string fieldName, string reason)
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
        /// 取得儲存格文字。
        /// </summary>
        private string GetCellValue(IRow row, Dictionary<string, int> headerMap, string headerName)
        {
            return headerMap.ContainsKey(headerName)
                ? row.GetCellData(headerMap[headerName]).Trim()
                : string.Empty;
        }

        /// <summary>
        /// 判斷是否為空白列。
        /// </summary>
        private bool IsEmptyRow(SjlShippingDataUploadModel item)
        {
            return string.IsNullOrWhiteSpace(item.JetfSerial)
                && string.IsNullOrWhiteSpace(item.BagNumber)
                && string.IsNullOrWhiteSpace(item.Seq)
                && string.IsNullOrWhiteSpace(item.Importer)
                && string.IsNullOrWhiteSpace(item.DeliveryDateText)
                && string.IsNullOrWhiteSpace(item.OtherFeeText)
                && string.IsNullOrWhiteSpace(item.CodText)
                && string.IsNullOrWhiteSpace(item.ImporterAddr)
                && string.IsNullOrWhiteSpace(item.ItemName)
                && string.IsNullOrWhiteSpace(item.QtyText)
                && string.IsNullOrWhiteSpace(item.VolumeText)
                && string.IsNullOrWhiteSpace(item.GwText)
                && string.IsNullOrWhiteSpace(item.ImporterPhone);
        }

        /// <summary>
        /// 解析日期。
        /// </summary>
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

        /// <summary>
        /// 解析整數。
        /// </summary>
        private int? ParseNullableInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            int intValue;
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out intValue))
            {
                return intValue;
            }

            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
            {
                return intValue;
            }

            decimal decimalValue;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue)
                || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
            {
                if (decimal.Truncate(decimalValue) == decimalValue)
                {
                    return Convert.ToInt32(decimalValue);
                }
            }

            return null;
        }

        /// <summary>
        /// 解析小數。
        /// </summary>
        private decimal? ParseNullableDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            decimal decimalValue;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue))
            {
                return decimalValue;
            }

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
            {
                return decimalValue;
            }

            return null;
        }

        /// <summary>
        /// 空字串轉為 null。
        /// </summary>
        private string NullIfEmpty(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        /// <summary>
        /// 建立查詢參數與分頁資訊。
        /// </summary>
        private DynamicParameters BuildSearchParameters(SjlBatchImportSearchRequest request)
        {
            request = request ?? new SjlBatchImportSearchRequest();

            DateTime startDate;
            DateTime endDate;
            DateTime? startDateValue = null;
            DateTime? endDateValue = null;

            if (!string.IsNullOrWhiteSpace(request.StartDate))
            {
                if (!DateTime.TryParse(request.StartDate, out startDate))
                {
                    throw new Exception("日期起格式不正確");
                }
                startDateValue = startDate.Date;
            }

            if (!string.IsNullOrWhiteSpace(request.EndDate))
            {
                if (!DateTime.TryParse(request.EndDate, out endDate))
                {
                    throw new Exception("日期迄格式不正確");
                }
                endDateValue = endDate.Date.AddDays(1);
            }

            if (startDateValue.HasValue && endDateValue.HasValue && startDateValue.Value >= endDateValue.Value)
            {
                throw new Exception("日期起不可大於日期迄");
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var parameters = new DynamicParameters();
            parameters.Add("StartDate", startDateValue);
            parameters.Add("EndDate", endDateValue);
            parameters.Add("JetfSerial", string.IsNullOrWhiteSpace(request.JetfSerial) ? string.Empty : request.JetfSerial.Trim());
            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);
            return parameters;
        }
    }
}
