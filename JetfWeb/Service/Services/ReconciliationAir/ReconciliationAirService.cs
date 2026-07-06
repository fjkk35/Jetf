using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using Service.Services.ReconciliationAir.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        /// <param name="filePath">Excel 檔案路徑。</param>
        /// <param name="type">資料來源類型（FTZ / TACT）。</param>
        /// <returns>上傳結果。</returns>
        public ResponseModel UploadAir(string filePath, string type)
        {
            try
            {
                var uploadRows = ReadExcelFile(filePath, type);
                if (uploadRows.Count == 0)
                {
                    return new ResponseModel("Excel 檔案中沒有資料");
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
        /// 讀取 Excel 檔案內容，依欄位名稱動態定位，FTZ 與 TACT 共用。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="type">資料來源類型。</param>
        /// <returns>上傳列資料。</returns>
        private static List<ReconciliationAirUploadRow> ReadExcelFile(string filePath, string type)
        {
            var uploadRows = new List<ReconciliationAirUploadRow>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
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
            return row;
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
