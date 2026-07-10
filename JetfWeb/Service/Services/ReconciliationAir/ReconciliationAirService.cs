using NPOI.SS.UserModel;
using Microsoft.VisualBasic.FileIO;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using Service.Services.ReconciliationAir.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Services.ReconciliationAir
{
    /// <summary>
    /// 空快代收銷帳服務。
    /// </summary>
    public class ReconciliationAirService : _BaseService
    {
        /// <summary>
        /// 建立空快代收銷帳服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReconciliationAirService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 上傳空快代收銷帳資料。
        /// </summary>
        /// <param name="filePath">上傳檔案路徑。</param>
        /// <param name="type">資料來源類型（FTZ / TACT）。</param>
        /// <returns>上傳結果。</returns>
        public ResponseModel UploadAir(string filePath, string type)
        {
            try
            {
                var uploadRows = ReadUploadFile(filePath, type);
                if (uploadRows.Count == 0)
                {
                    return new ResponseModel("檔案中沒有資料");
                }

                ValidateRows(uploadRows);

                var failRows = uploadRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.FailReason))
                    .ToList();

                if (failRows.Any())
                {
                    var failResult = new ReconciliationAirUploadResult
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

                var successResult = UpsertAir(uploadRows);
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
        /// 依資料來源與副檔名讀取上傳檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="type">資料來源類型。</param>
        /// <returns>上傳列資料。</returns>
        private static List<ReconciliationAirUploadRow> ReadUploadFile(string filePath, string type)
        {
            var extension = Path.GetExtension(filePath);

            if (IsTact(type) && string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ReadCsvFile(filePath, type);
            }

            if (IsFtz(type) && IsExcelExtension(extension))
            {
                return ReadExcelFile(filePath, type);
            }

            throw new InvalidOperationException(GetFileTypeErrorMessage(type));
        }

        /// <summary>
        /// 讀取 Excel 檔案內容，依欄位名稱動態定位。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="type">資料來源類型。</param>
        /// <returns>上傳列資料。</returns>
        private static List<ReconciliationAirUploadRow> ReadExcelFile(string filePath, string type)
        {
            var uploadRows = new List<ReconciliationAirUploadRow>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = CreateWorkbook(stream, Path.GetExtension(filePath));
                ISheet sheet = workbook.GetSheetAt(0);

                if (!TryGetHeaderColumnIndex(sheet, out int headerRowIndex, out Dictionary<string, int> colIndex))
                {
                    return uploadRows;
                }

                for (int i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    var uploadRow = NormalizeRow(new ReconciliationAirUploadRow
                    {
                        RowNo = i + 1,
                        Type = type,
                        MainNumber = GetCellByName(row, colIndex, "主號"),
                        TrackingNo = GetCellByName(row, colIndex, "分號"),
                        Recipient = GetCellByName(row, colIndex, "納稅義務人"),
                        TaxRecId = GetCellByName(row, colIndex, "納稅義務人統一編號"),
                        TaxBaseText = GetCellByName(row, colIndex, "營業稅基"),
                        TaxText = GetCellByName(row, colIndex, "稅費金額")
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
        /// 讀取 CSV 檔案內容，依欄位名稱動態定位。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="type">資料來源類型。</param>
        /// <returns>上傳列資料。</returns>
        private static List<ReconciliationAirUploadRow> ReadCsvFile(string filePath, string type)
        {
            var uploadRows = new List<ReconciliationAirUploadRow>();
            Dictionary<string, int> colIndex = null;
            var rowNo = 0;

            using (var parser = CreateCsvParser(filePath))
            {
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    rowNo++;

                    if (fields == null)
                    {
                        continue;
                    }

                    if (colIndex == null)
                    {
                        var candidate = BuildColumnIndex(fields);
                        if (!IsUploadHeader(candidate))
                        {
                            continue;
                        }

                        colIndex = candidate;
                        continue;
                    }

                    var uploadRow = NormalizeRow(new ReconciliationAirUploadRow
                    {
                        RowNo = rowNo,
                        Type = type,
                        MainNumber = GetFieldByName(fields, colIndex, "主號"),
                        TrackingNo = GetFieldByName(fields, colIndex, "分號"),
                        Recipient = GetFieldByName(fields, colIndex, "納稅義務人"),
                        TaxRecId = GetFieldByName(fields, colIndex, "納稅義務人統一編號"),
                        TaxBaseText = GetFieldByName(fields, colIndex, "營業稅基"),
                        TaxText = GetFieldByName(fields, colIndex, "稅費金額")
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
        /// 建立 Excel 活頁簿。
        /// </summary>
        /// <param name="stream">檔案串流。</param>
        /// <param name="extension">副檔名。</param>
        /// <returns>Excel 活頁簿。</returns>
        private static IWorkbook CreateWorkbook(Stream stream, string extension)
        {
            if (string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                return new HSSFWorkbook(stream);
            }

            if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return new XSSFWorkbook(stream);
            }

            throw new InvalidOperationException("副檔名需為 xls 或 xlsx");
        }

        /// <summary>
        /// 建立 CSV 解析器。
        /// </summary>
        /// <param name="filePath">CSV 檔案路徑。</param>
        /// <returns>CSV 解析器。</returns>
        private static TextFieldParser CreateCsvParser(string filePath)
        {
            var parser = new TextFieldParser(filePath, DetectCsvEncoding(filePath));
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = false;
            return parser;
        }

        /// <summary>
        /// 偵測 CSV 編碼，沒有 BOM 時使用系統預設編碼。
        /// </summary>
        /// <param name="filePath">CSV 檔案路徑。</param>
        /// <returns>文字編碼。</returns>
        private static Encoding DetectCsvEncoding(string filePath)
        {
            var bom = new byte[3];
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Read(bom, 0, bom.Length);
            }

            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                return Encoding.UTF8;
            }

            if (bom[0] == 0xFF && bom[1] == 0xFE)
            {
                return Encoding.Unicode;
            }

            if (bom[0] == 0xFE && bom[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }

            return Encoding.Default;
        }

        /// <summary>
        /// 掃描工作表並找出上傳資料的標題列。
        /// </summary>
        /// <param name="sheet">Excel 工作表。</param>
        /// <param name="headerRowIndex">標題列索引。</param>
        /// <param name="colIndex">欄位名稱對應表。</param>
        /// <returns>是否找到標題列。</returns>
        private static bool TryGetHeaderColumnIndex(ISheet sheet, out int headerRowIndex, out Dictionary<string, int> colIndex)
        {
            headerRowIndex = -1;
            colIndex = null;

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row == null)
                {
                    continue;
                }

                var candidate = BuildColumnIndex(row);
                if (!IsUploadHeader(candidate))
                {
                    continue;
                }

                headerRowIndex = i;
                colIndex = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判斷欄位名稱是否符合空運銷帳上傳表頭。
        /// </summary>
        /// <param name="colIndex">欄位名稱對應表。</param>
        /// <returns>是否為上傳表頭。</returns>
        private static bool IsUploadHeader(Dictionary<string, int> colIndex)
        {
            return colIndex.ContainsKey("主號")
                && colIndex.ContainsKey("分號");
        }

        /// <summary>
        /// 依標題列建立欄位名稱到索引的對應表。
        /// </summary>
        /// <param name="headerRow">標題列。</param>
        /// <returns>欄位名稱對應表。</returns>
        private static Dictionary<string, int> BuildColumnIndex(IRow headerRow)
        {
            var colIndex = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int c = 0; c < headerRow.LastCellNum; c++)
            {
                var cell = headerRow.GetCell(c);
                if (cell == null)
                {
                    continue;
                }

                var name = cell.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name) && !colIndex.ContainsKey(name))
                {
                    colIndex[name] = c;
                }
            }

            return colIndex;
        }

        /// <summary>
        /// 依 CSV 標題列建立欄位名稱到索引的對應表。
        /// </summary>
        /// <param name="headerFields">CSV 標題欄位。</param>
        /// <returns>欄位名稱對應表。</returns>
        private static Dictionary<string, int> BuildColumnIndex(string[] headerFields)
        {
            var colIndex = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int c = 0; c < headerFields.Length; c++)
            {
                var name = headerFields[c]?.Trim();
                if (!string.IsNullOrEmpty(name) && !colIndex.ContainsKey(name))
                {
                    colIndex[name] = c;
                }
            }

            return colIndex;
        }

        /// <summary>
        /// 依欄位名稱取得儲存格內容。
        /// </summary>
        /// <param name="row">列。</param>
        /// <param name="colIndex">欄位名稱對應表。</param>
        /// <param name="columnName">欄位名稱。</param>
        /// <returns>儲存格文字，找不到欄位時回傳空字串。</returns>
        private static string GetCellByName(IRow row, Dictionary<string, int> colIndex, string columnName)
        {
            if (!colIndex.TryGetValue(columnName, out int idx))
            {
                return string.Empty;
            }

            var cell = row.GetCell(idx);
            if (cell == null)
            {
                return string.Empty;
            }

            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
            {
                return cell.DateCellValue.ToString("yyyy-MM-dd");
            }

            return cell.ToString()?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 依欄位名稱取得 CSV 欄位內容。
        /// </summary>
        /// <param name="fields">CSV 欄位值。</param>
        /// <param name="colIndex">欄位名稱對應表。</param>
        /// <param name="columnName">欄位名稱。</param>
        /// <returns>欄位文字，找不到欄位時回傳空字串。</returns>
        private static string GetFieldByName(string[] fields, Dictionary<string, int> colIndex, string columnName)
        {
            if (!colIndex.TryGetValue(columnName, out int idx) || idx >= fields.Length)
            {
                return string.Empty;
            }

            return fields[idx]?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 判斷是否為 TACT 資料來源。
        /// </summary>
        /// <param name="type">資料來源類型。</param>
        /// <returns>是否為 TACT。</returns>
        private static bool IsTact(string type)
        {
            return string.Equals(type, "TACT", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判斷是否為 FTZ 資料來源。
        /// </summary>
        /// <param name="type">資料來源類型。</param>
        /// <returns>是否為 FTZ。</returns>
        private static bool IsFtz(string type)
        {
            return string.Equals(type, "FTZ", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判斷是否為支援的 Excel 副檔名。
        /// </summary>
        /// <param name="extension">副檔名。</param>
        /// <returns>是否為 Excel 副檔名。</returns>
        private static bool IsExcelExtension(string extension)
        {
            return string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得資料來源對應的副檔名錯誤訊息。
        /// </summary>
        /// <param name="type">資料來源類型。</param>
        /// <returns>錯誤訊息。</returns>
        private static string GetFileTypeErrorMessage(string type)
        {
            if (IsTact(type))
            {
                return "TACT-華儲上傳檔案副檔名需為 csv";
            }

            if (IsFtz(type))
            {
                return "FTZ-遠雄上傳檔案副檔名需為 xls 或 xlsx";
            }

            return "資料來源需為 FTZ 或 TACT";
        }

        /// <summary>
        /// 正規化單筆上傳資料。
        /// </summary>
        /// <param name="row">原始資料。</param>
        /// <returns>正規化後資料。</returns>
        private static ReconciliationAirUploadRow NormalizeRow(ReconciliationAirUploadRow row)
        {
            row.Type = row.Type?.Trim() ?? string.Empty;
            row.MainNumber = row.MainNumber?.Trim() ?? string.Empty;
            row.TrackingNo = row.TrackingNo?.Trim() ?? string.Empty;
            row.Recipient = row.Recipient?.Trim() ?? string.Empty;
            row.TaxRecId = row.TaxRecId?.Trim() ?? string.Empty;
            row.TaxBaseText = row.TaxBaseText?.Trim() ?? string.Empty;
            row.TaxText = row.TaxText?.Trim() ?? string.Empty;
            row.FailReason = row.FailReason?.Trim() ?? string.Empty;

            if (IsTact(row.Type))
            {
                row.MainNumber = RemoveSingleQuotes(row.MainNumber);
                row.TrackingNo = RemoveSingleQuotes(row.TrackingNo);
                row.TaxRecId = RemoveSingleQuotes(row.TaxRecId);
            }

            return row;
        }

        /// <summary>
        /// 移除單號中的單引號。
        /// </summary>
        /// <param name="value">單號文字。</param>
        /// <returns>移除單引號後的單號文字。</returns>
        private static string RemoveSingleQuotes(string value)
        {
            return (value ?? string.Empty).Replace("'", string.Empty).Trim();
        }

        /// <summary>
        /// 判斷是否為空白列。
        /// </summary>
        /// <param name="row">列資料。</param>
        /// <returns>是否空白。</returns>
        private static bool IsEmptyRow(ReconciliationAirUploadRow row)
        {
            return string.IsNullOrWhiteSpace(row.MainNumber)
                && string.IsNullOrWhiteSpace(row.TrackingNo)
                && string.IsNullOrWhiteSpace(row.Recipient)
                && string.IsNullOrWhiteSpace(row.TaxRecId);
        }

        /// <summary>
        /// 驗證上傳資料。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        private static void ValidateRows(List<ReconciliationAirUploadRow> uploadRows)
        {
            foreach (var row in uploadRows)
            {
                var failReasons = new List<string>();

                if (string.IsNullOrWhiteSpace(row.TrackingNo))
                {
                    failReasons.Add("分號必填");
                }

                if (string.IsNullOrWhiteSpace(row.Type))
                {
                    failReasons.Add("類型必填");
                }

                if (!string.IsNullOrWhiteSpace(row.TaxBaseText))
                {
                    if (int.TryParse(row.TaxBaseText, out var taxBase))
                    {
                        row.TaxBase = taxBase;
                    }
                    else
                    {
                        failReasons.Add("營業稅基格式錯誤");
                    }
                }

                if (!string.IsNullOrWhiteSpace(row.TaxText))
                {
                    if (int.TryParse(row.TaxText, out var tax))
                    {
                        row.Tax = tax;
                    }
                    else
                    {
                        failReasons.Add("稅費金額格式錯誤");
                    }
                }

                row.FailReason = string.Join("；", failReasons);
            }
        }

        /// <summary>
        /// 以分號為鍵值替換空快代收銷帳資料。
        /// </summary>
        /// <param name="uploadRows">上傳列資料。</param>
        /// <returns>寫入結果。</returns>
        private ReconciliationAirUploadResult UpsertAir(List<ReconciliationAirUploadRow> uploadRows)
        {
            var currentUserId = GetUserId();
            var currentTime = DateTime.Now;

            var trackingNos = uploadRows
                .Select(x => x.TrackingNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var existingEntities = JetfDb.ReconciliationAirs
                .WhereBulkContains(JetfDb, trackingNos, x => x.TrackingNo, x => x);

            var existingTrackingNos = new HashSet<string>(
                existingEntities
                    .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                    .Select(x => x.TrackingNo),
                StringComparer.OrdinalIgnoreCase);

            var createdCount = uploadRows.Count(x => !existingTrackingNos.Contains(x.TrackingNo));
            var updatedCount = uploadRows.Count - createdCount;
            var insertEntities = uploadRows
                .Select(row => new ReconciliationAirEntity
                {
                    Type = row.Type,
                    MainNumber = row.MainNumber,
                    TrackingNo = row.TrackingNo,
                    Recipient = row.Recipient,
                    TaxRecId = row.TaxRecId,
                    TaxBase = row.TaxBase,
                    Tax = row.Tax,
                    CreatedOpe = currentUserId,
                    CreatedTime = currentTime
                })
                .ToList();

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    if (trackingNos.Any())
                    {
                        JetfDb.DeleteByColumnValues<ReconciliationAirEntity, string>(
                            trackingNos,
                            x => x.TrackingNo);
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

            return new ReconciliationAirUploadResult
            {
                Count = uploadRows.Count,
                CreatedCount = createdCount,
                UpdatedCount = updatedCount,
                Message = $"上傳完成，共 {uploadRows.Count} 筆，新增 {createdCount} 筆，更新 {updatedCount} 筆"
            };
        }
    }
}
