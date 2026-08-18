using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Services.AirMainComparison.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.AirMainComparison
{
    /// <summary>
    /// FTZ 與 Tact 共用的空運主號上傳解析、比對、統計及匯出服務。
    /// </summary>
    public class AirMainComparisonService
    {
        /// <summary>
        /// 查不到派件公司時，匯出統計要歸到這個固定欄位。
        /// </summary>
        private const string NoTransName = "無派件公司";

        /// <summary>
        /// 上傳明細中需要另外統計的備註值。
        /// </summary>
        private const string ZzzaRemark = "ZZZA";

        /// <summary>
        /// AIR_DETAIN 的 G 類無 ID 顯示文字。
        /// </summary>
        private const string GTypeNoIdStatus = "G類無ID";

        private readonly DataCenterDbContext _dataCenterDb;
        private readonly SqlConnection _connection;

        /// <summary>
        /// 建立空運主號共用比對服務。
        /// </summary>
        /// <param name="dataCenterDb">DATA_CENTER 資料庫內容。</param>
        /// <param name="connection">由 DI 管理的 JETF 資料庫連線。</param>
        public AirMainComparisonService(
            DataCenterDbContext dataCenterDb,
            SqlConnection connection)
        {
            _dataCenterDb = dataCenterDb ?? throw new ArgumentNullException(nameof(dataCenterDb));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// 讀取空運主號比對上傳 Excel。
        /// 讀取主號查詢上傳 Excel 的明細資料。
        /// </summary>
        /// <param name="uploadStream">上傳檔案串流。</param>
        /// <returns>完整上傳資料。</returns>
        public AirMainUploadExcelData ReadUploadData(Stream uploadStream)
        {
            var uploadData = new AirMainUploadExcelData();
            if (uploadStream == null)
            {
                return uploadData;
            }

            if (uploadStream.CanSeek)
            {
                // 同一個檔案可能先被查詢流程讀過，再被匯出流程重讀，先把串流位置歸零。
                uploadStream.Position = 0;
                if (uploadStream.Length == 0)
                {
                    throw new Exception("上傳檔案內容為空，請重新選擇檔案後再匯出");
                }
            }

            // 同一個上傳檔只建立一次 workbook，避免重複讀取 HttpPostedFileBase.InputStream 時發生空串流。
            IWorkbook workbook;
            try
            {
                workbook = WorkbookFactory.Create(uploadStream);
            }
            catch (Exception ex)
            {
                throw new Exception($"讀取 Excel 失敗：{ex.Message}");
            }

            uploadData.DetailRows = ReadUploadDetailRows(workbook);
            uploadData.SummaryRows = ReadUploadSummaryRows(workbook);
            return uploadData;
        }

        /// <summary>
        /// 套用空運主號的客戶、派件公司、未收單、ZZZA、錯單及狀態規則。
        /// 套用主號上傳明細的未收單與 ZZZA 統計。
        /// </summary>
        /// <param name="results">FTZ 或 Tact 主號查詢結果。</param>
        /// <param name="uploadRows">上傳明細資料。</param>
        /// <param name="excludeZzzaFromUnreceivedB6F">是否從未收單 B6F 統計排除 ZZZA。</param>
        public void ApplyComparison(
            IList<IAirMainComparisonItem> results,
            IEnumerable<AirMainUploadExcelRow> uploadRows,
            bool excludeZzzaFromUnreceivedB6F = false)
        {
            if (results == null)
            {
                return;
            }

            // 收集所有需要查詢的主號、分號、袋號。
            // 建立字典存放查詢結果。
            SetCustomerNames(results);
            // 設定派件公司。
            SetDetailTransNames(results);

            var uploadRowsByMwb = BuildUploadRowsByMwb(uploadRows);
            var unreceivedRowsByItem = GetUnreceivedRowsByItem(results, uploadRowsByMwb);
            var allUnreceivedRows = unreceivedRowsByItem.SelectMany(x => x.Value).ToList();

            SetUnreceivedTransNames(allUnreceivedRows);
            SetPlinkErrors(allUnreceivedRows);

            var airDetainStatusLookup = GetAirDetainStatusLookup(results, allUnreceivedRows);

            foreach (var item in results)
            {
                List<AirMainUploadExcelRow> unreceivedRows;
                if (!unreceivedRowsByItem.TryGetValue(item, out unreceivedRows))
                {
                    unreceivedRows = new List<AirMainUploadExcelRow>();
                }

                // 上傳檔標記 ZZZA，且能對應到 FTZ 未進倉明細的資料，視為「ZZZA收單」。
                var zzzaReceivedRows = GetZzzaReceivedRows(item, uploadRowsByMwb);
                // 上傳檔標記 ZZZA、有出現在 FTZ 主號查詢資料，且不在未進倉明細，視為「ZZZA進倉」。
                var zzzaGciRows = GetZzzaGciRows(item, uploadRowsByMwb);

                foreach (var detail in item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                {
                    // 先將 ZZZA 註記寫入明細模型，匯出時只需輸出模型值，不再重新比對。
                    detail.ZzzaRemark = GetZzzaRemark(detail, zzzaReceivedRows);
                    detail.Status = GetAirDetainStatus(airDetainStatusLookup, detail.Hwb);
                }

                foreach (var unreceivedRow in unreceivedRows)
                {
                    unreceivedRow.Status = GetAirDetainStatus(airDetainStatusLookup, unreceivedRow.BagNo);
                }

                // 上傳檔標記 ZZZA，且未出現在 FTZ 查詢結果的資料，視為「ZZZA未收單」。
                item.ZzzaGciCount = zzzaGciRows.Count;
                item.ZzzaReceivedCount = zzzaReceivedRows.Count;
                item.ZzzaUnreceivedCount = unreceivedRows.Count(IsZzzaUploadRow);
                item.ZzzaCount = item.ZzzaGciCount + item.ZzzaReceivedCount + item.ZzzaUnreceivedCount;

                // 未收單件數只統計有派件公司的非 ZZZA 補列資料；無派件公司另列於派件公司統計。
                item.UnreceivedCount = unreceivedRows.Count(row =>
                    !IsZzzaUploadRow(row) && !IsSameTransName(row.TransName, NoTransName));
                item.UnreceivedRows = unreceivedRows;
                item.UnreceivedB6FCount = unreceivedRows.Count(row =>
                    (!excludeZzzaFromUnreceivedB6F || !IsZzzaUploadRow(row)) &&
                    (row.PlinkErrors ?? new List<AirMainPlinkErrorRow>())
                        .Any(error => ContainsB6FReason(error.Reason)));

                // G類無ID只計算未收單補列資料，不包含原本已收單的未進倉明細。
                item.GTypeNoIdCount = unreceivedRows.Count(row =>
                    string.Equals(row.Status, GTypeNoIdStatus, StringComparison.OrdinalIgnoreCase));

                // 收單件數與申報不計入 ZZZA進倉、ZZZA收單；進倉不計入 ZZZA進倉。
                item.ReceivedPieceCount =
                    item.DeclaredPiece + item.BagCount - item.ZzzaGciCount - item.ZzzaReceivedCount;
                item.DeclaredPiece = item.DeclaredPiece - item.ZzzaGciCount - item.ZzzaReceivedCount;
                item.GciPiece = item.GciPiece - item.ZzzaGciCount;
                // 未進倉件及未進倉小計只需排除仍在未進倉明細內的 ZZZA收單。
                item.NotGciPiece = item.NotGciPiece - item.ZzzaReceivedCount;
                item.NotGciTotal = item.NotGciTotal - item.ZzzaReceivedCount;

                // 派件公司統計同樣排除 ZZZA收單及 ZZZA未收單，匯出直接使用此計算結果。
                item.TransNameCounts = BuildTransNameCounts(item, unreceivedRows);
                item.TransNameSummary = BuildTransNameSummary(item.TransNameCounts);
            }
        }

        /// <summary>
        /// 建立空運主號比對結果 Excel。
        /// </summary>
        /// <param name="mainSheetName">第一頁頁籤名稱。</param>
        /// <param name="results">已套用共同比對規則的結果。</param>
        /// <param name="uploadData">完整上傳資料。</param>
        /// <returns>輸出 Workbook。</returns>
        public IWorkbook CreateExportWorkbook(
            string mainSheetName,
            IEnumerable<IAirMainComparisonItem> results,
            AirMainUploadExcelData uploadData)
        {
            // 主號查詢階段已完成 ZZZA 統計與扣除，匯出只負責寫入計算完成的資料。
            var resultList = (results ?? Enumerable.Empty<IAirMainComparisonItem>()).ToList();
            uploadData = uploadData ?? new AirMainUploadExcelData();

            // 建立 Excel。
            IWorkbook workbook = new XSSFWorkbook();
            // 建立樣式。
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            // 先把上傳資料整理成實際需要補到未進倉明細的未收單資料，供兩個頁籤共用。
            var uploadTotalPieceByMwb = BuildUploadTotalPieceByMwb(uploadData.SummaryRows);
            var uploadSummaryByMwb = BuildUploadSummaryByMwb(uploadData.SummaryRows);

            CreateMainResultSheet(
                workbook,
                string.IsNullOrWhiteSpace(mainSheetName) ? "主號查詢結果" : mainSheetName,
                resultList,
                uploadTotalPieceByMwb,
                uploadSummaryByMwb,
                headerStyle,
                dataStyle,
                numberStyle);

            CreateNotGciDetailSheet(workbook, resultList, headerStyle, dataStyle);
            return workbook;
        }

        /// <summary>
        /// 讀取主號查詢上傳 Excel 的明細頁籤。
        /// </summary>
        /// <returns>分艙單收單註記為 X 的資料列。</returns>
        private List<AirMainUploadExcelRow> ReadUploadDetailRows(IWorkbook workbook)
        {
            // 需求只接受「明細」頁籤，其他頁籤資料一律不處理。
            var sheet = workbook.GetSheet("明細");
            if (sheet == null)
            {
                throw new Exception("找不到 Excel 頁籤：明細");
            }

            // 先定位表頭，之後才能依欄名讀取主號、袋號與收單註記。
            var requiredHeaders = new[] { "袋號", "主號", "分艙單收單註記", "1分號多件之分號", "備註" };
            var headerInfo = FindUploadHeader(sheet, requiredHeaders);
            var headerMap = headerInfo.Item2;
            var missingHeaders = requiredHeaders.Where(header => !headerMap.ContainsKey(header)).ToList();
            if (missingHeaders.Any())
            {
                throw new Exception($"明細頁籤缺少欄位：{string.Join("、", missingHeaders)}");
            }

            var uploadRows = new List<AirMainUploadExcelRow>();
            for (int rowIndex = headerInfo.Item1 + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var bagNo = row.GetCellData(headerMap["袋號"]);
                var mwb = row.GetCellData(headerMap["主號"]);
                var receiptMark = row.GetCellData(headerMap["分艙單收單註記"]);
                var oneHwbMultiPieceHwb = row.GetCellData(headerMap["1分號多件之分號"]);
                var remark = row.GetCellData(headerMap["備註"]);

                // 只有收單註記為 X 的資料，才需要納入後續未收單比對。
                // 主號或袋號缺值時無法比對，直接略過。
                if (!string.Equals(receiptMark, "X", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(bagNo) ||
                    string.IsNullOrWhiteSpace(mwb))
                {
                    continue;
                }

                uploadRows.Add(new AirMainUploadExcelRow
                {
                    BagNo = bagNo.Trim(),
                    Mwb = mwb.Trim(),
                    ReceiptMark = receiptMark.Trim(),
                    OneHwbMultiPieceHwb = (oneHwbMultiPieceHwb ?? "").Trim(),
                    Remark = IsZzzaRemark(remark) ? ZzzaRemark : ""
                });
            }

            return uploadRows;
        }

        /// <summary>
        /// 讀取主號查詢上傳 Excel 的主號2 頁籤。
        /// </summary>
        private List<AirMainUploadSummaryRow> ReadUploadSummaryRows(IWorkbook workbook)
        {
            var sheet = workbook.GetSheet("主號2");
            if (sheet == null)
            {
                throw new Exception("找不到 Excel 頁籤：主號2");
            }

            var requiredHeaders = new[] { "主號", "總件數", "傳輸時間", "進口日期", "航機班次" };
            var headerInfo = FindUploadHeader(sheet, requiredHeaders);
            var headerMap = headerInfo.Item2;
            var missingHeaders = requiredHeaders.Where(header => !headerMap.ContainsKey(header)).ToList();
            if (missingHeaders.Any())
            {
                throw new Exception($"主號2頁籤缺少欄位：{string.Join("、", missingHeaders)}");
            }

            var summaryRows = new List<AirMainUploadSummaryRow>();
            for (int rowIndex = headerInfo.Item1 + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var mwb = row.GetCellData(headerMap["主號"]);
                if (string.IsNullOrWhiteSpace(mwb))
                {
                    continue;
                }

                summaryRows.Add(new AirMainUploadSummaryRow
                {
                    Mwb = mwb.Trim(),
                    TotalPiece = (row.GetCellData(headerMap["總件數"]) ?? "").Trim(),
                    TransmissionTime = (row.GetCellData(headerMap["傳輸時間"]) ?? "").Trim(),
                    ImportDate = (row.GetCellData(headerMap["進口日期"]) ?? "").Trim(),
                    FlightNumber = (row.GetCellData(headerMap["航機班次"]) ?? "").Trim()
                });
            }

            return summaryRows;
        }

        /// <summary>
        /// 尋找上傳 Excel 的表頭列。
        /// 尋找主號上傳 Excel 的表頭列。
        /// </summary>
        /// <param name="sheet">工作表。</param>
        /// <param name="requiredHeaders">必要欄位。</param>
        /// <returns>表頭列索引與欄位對照。</returns>
        private Tuple<int, Dictionary<string, int>> FindUploadHeader(ISheet sheet, string[] requiredHeaders)
        {
            var bestHeaderRowIndex = -1;
            var bestHeaderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var bestMatchCount = 0;

            for (int rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || row.LastCellNum < 0)
                {
                    continue;
                }

                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var startCellIndex = row.FirstCellNum < 0 ? 0 : row.FirstCellNum;
                for (int cellIndex = startCellIndex; cellIndex < row.LastCellNum; cellIndex++)
                {
                    var headerName = row.GetCellData(cellIndex);
                    if (string.IsNullOrWhiteSpace(headerName) || headerMap.ContainsKey(headerName))
                    {
                        continue;
                    }

                    headerMap.Add(headerName.Trim(), cellIndex);
                }

                // 有些檔案前面會有說明列，這裡挑出最符合需求欄位數的那一列當表頭。
                var matchCount = requiredHeaders.Count(header => headerMap.ContainsKey(header));
                if (matchCount > bestMatchCount)
                {
                    bestMatchCount = matchCount;
                    bestHeaderRowIndex = rowIndex;
                    bestHeaderMap = headerMap;
                }

                if (matchCount == requiredHeaders.Length)
                {
                    break;
                }
            }

            return Tuple.Create(bestHeaderRowIndex, bestHeaderMap);
        }

        /// <summary>
        /// 批次查詢主號的客戶名稱。
        /// </summary>
        private void SetCustomerNames(IEnumerable<IAirMainComparisonItem> results)
        {
            // 收集所有需要查詢的主號。
            var mwbs = (results ?? Enumerable.Empty<IAirMainComparisonItem>())
                .Select(item => item.Mwb)
                .Where(mwb => !string.IsNullOrWhiteSpace(mwb))
                .Select(mwb => mwb.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!mwbs.Any())
            {
                return;
            }

            // 1. 批次查詢客戶名稱。
            const string sql = @"
                SELECT a.MAINNUMBER, b.DESPATCHNAME
                FROM [DATA_CENTER].[dbo].[MAINORDERINFO] a
                JOIN [DATA_CENTER].[dbo].[DESPATCHFROM] b ON a.DELIVERYFROM = b.DESPATCHNO
                WHERE a.MAINNUMBER IN @Mwbs
                GROUP BY a.MAINNUMBER, b.DESPATCHNAME";

            var customerLookup = _connection
                .Query<(string MainNumber, string CustomerName)>(sql, new { Mwbs = mwbs })
                .Where(row => !string.IsNullOrWhiteSpace(row.MainNumber))
                .GroupBy(row => row.MainNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(row => row.CustomerName).FirstOrDefault(),
                    StringComparer.OrdinalIgnoreCase);

            // 使用字典填充資料。
            foreach (var item in results)
            {
                // 設定客戶名稱。
                string customer;
                var mwb = (item.Mwb ?? "").Trim();
                if (customerLookup.TryGetValue(mwb, out customer))
                {
                    item.Customer = customer;
                }
            }
        }

        /// <summary>
        /// 取得未進倉明細的派件公司。
        /// </summary>
        private void SetDetailTransNames(IEnumerable<IAirMainComparisonItem> results)
        {
            // 收集所有需要查詢的分號、袋號。
            var details = (results ?? Enumerable.Empty<IAirMainComparisonItem>())
                .SelectMany(item => item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                .ToList();

            var hwbNos = details
                .Where(detail => string.IsNullOrWhiteSpace(detail.BagNo))
                .Select(detail => detail.Hwb)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var bagNos = details
                .Where(detail => !string.IsNullOrWhiteSpace(detail.BagNo))
                .Select(detail => detail.BagNo)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, string> transNameLookup = null;
            var hwbLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bagLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (hwbNos.Any())
            {
                // 2. 批次查詢分提單號的派件公司。
                // 3. 批次查詢分提單號的派件公司。
                transNameLookup = GetAirTransNameLookup();
                hwbLookup = GetOriginalTransNameLookup(hwbNos, true, true, transNameLookup);
            }

            if (bagNos.Any())
            {
                // 3. 批次查詢袋號的派件公司。
                // 4. 批次查詢袋號的派件公司。
                transNameLookup = transNameLookup ?? GetAirTransNameLookup();
                bagLookup = GetOriginalTransNameLookup(bagNos, false, true, transNameLookup);
            }

            foreach (var detail in details)
            {
                string transName;
                if (string.IsNullOrWhiteSpace(detail.BagNo))
                {
                    // 用分提單號查詢。
                    var hwb = (detail.Hwb ?? "").Trim();
                    if (hwbLookup.TryGetValue(hwb, out transName))
                    {
                        detail.TransName = transName;
                    }
                }
                else
                {
                    // 用袋號查詢。
                    var bagNo = detail.BagNo.Trim();
                    if (bagLookup.TryGetValue(bagNo, out transName))
                    {
                        detail.TransName = transName;
                    }
                }
            }
        }

        /// <summary>
        /// 取得空運派件代碼與派件公司名稱對照。
        /// </summary>
        private Dictionary<string, string> GetAirTransNameLookup()
        {
            const string sql = @"
                SELECT TRANS_NO, TRANS_NAME
                FROM [jetf].[dbo].[customer_master]
                WHERE TRAN_TYPE = N'空運'";

            return _connection.Query<(string TransNo, string TransName)>(sql)
                .Where(row => !string.IsNullOrWhiteSpace(row.TransNo))
                .GroupBy(row => row.TransNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(row => row.TransName).FirstOrDefault(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依分號或袋號批次查詢派件公司名稱。
        /// </summary>
        private Dictionary<string, string> GetOriginalTransNameLookup(
            IEnumerable<string> values,
            bool includeTrackingNo,
            bool includeBagNo,
            Dictionary<string, string> transNameLookup = null)
        {
            var keys = (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!keys.Any() || (!includeTrackingNo && !includeBagNo))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var originalRows = new List<AirMainOriginalTransNameRow>();
            if (includeTrackingNo)
            {
                originalRows.AddRange(_dataCenterDb.OriginalLists
                    .AsNoTracking()
                    .WhereBulkContains(
                        _dataCenterDb,
                        keys,
                        row => row.TrackingNo,
                        key => key,
                        row => new AirMainOriginalTransNameRow
                        {
                            TrackingNo = row.TrackingNo,
                            TransNo = row.ClearanceWarehousing
                        }));
            }

            if (includeBagNo)
            {
                originalRows.AddRange(_dataCenterDb.OriginalLists
                    .AsNoTracking()
                    .WhereBulkContains(
                        _dataCenterDb,
                        keys,
                        row => row.BagNo,
                        key => key,
                        row => new AirMainOriginalTransNameRow
                        {
                            TrackingNo = row.BagNo,
                            TransNo = row.ClearanceWarehousing
                        }));
            }

            transNameLookup = transNameLookup ?? GetAirTransNameLookup();
            return originalRows
                .Where(row => !string.IsNullOrWhiteSpace(row.TrackingNo))
                .GroupBy(row => row.TrackingNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => GetTransNameByTransNo(
                        transNameLookup,
                        group.Select(row => row.TransNo.HasValue
                            ? row.TransNo.Value.ToString()
                            : "").FirstOrDefault()),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依派件代碼取得派件公司名稱。
        /// </summary>
        private string GetTransNameByTransNo(Dictionary<string, string> lookup, string transNo)
        {
            if (lookup == null || string.IsNullOrWhiteSpace(transNo))
            {
                return "";
            }

            string transName;
            return lookup.TryGetValue(transNo.Trim(), out transName) ? transName : "";
        }

        /// <summary>
        /// 依主號整理主號查詢上傳資料。
        /// </summary>
        private Dictionary<string, List<AirMainUploadExcelRow>> BuildUploadRowsByMwb(
            IEnumerable<AirMainUploadExcelRow> uploadRows)
        {
            return (uploadRows ?? Enumerable.Empty<AirMainUploadExcelRow>())
                .Where(row => !string.IsNullOrWhiteSpace(row.Mwb) && !string.IsNullOrWhiteSpace(row.BagNo))
                .GroupBy(row => row.Mwb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(row => row.BagNo.Trim(), StringComparer.OrdinalIgnoreCase)
                        .Select(rowGroup => rowGroup.OrderByDescending(IsZzzaUploadRow).First())
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得需要補到未進倉明細的未收單上傳資料。
        /// </summary>
        private Dictionary<IAirMainComparisonItem, List<AirMainUploadExcelRow>> GetUnreceivedRowsByItem(
            IEnumerable<IAirMainComparisonItem> results,
            Dictionary<string, List<AirMainUploadExcelRow>> uploadRowsByMwb)
        {
            var unreceivedRowsByItem = new Dictionary<IAirMainComparisonItem, List<AirMainUploadExcelRow>>();
            foreach (var item in results ?? Enumerable.Empty<IAirMainComparisonItem>())
            {
                var mwb = (item.Mwb ?? "").Trim();
                List<AirMainUploadExcelRow> mainUploadRows;
                if (!string.IsNullOrWhiteSpace(item.ErrorMessage) ||
                    uploadRowsByMwb == null ||
                    !uploadRowsByMwb.TryGetValue(mwb, out mainUploadRows))
                {
                    continue;
                }

                var knownHwbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var knownBagNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var detail in item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                {
                    AddKnownValue(knownHwbs, detail.Hwb);
                    AddKnownValue(knownBagNos, detail.BagNo);
                }

                foreach (var queryRow in item.QueryRows ?? Enumerable.Empty<IAirMainQueryRow>())
                {
                    AddKnownValue(knownHwbs, queryRow.Hwb);
                    // 主號查詢結果已有併袋袋號時，上傳同袋號不列為未收單。
                    AddKnownValue(knownBagNos, queryRow.ExpBagNo);
                }

                var candidates = mainUploadRows
                    .Where(row =>
                    {
                        var bagNo = (row.BagNo ?? "").Trim();
                        return !string.IsNullOrWhiteSpace(bagNo) &&
                            !knownHwbs.Contains(bagNo) &&
                            !knownBagNos.Contains(bagNo);
                    })
                    .ToList();

                unreceivedRowsByItem[item] = FilterInvalidMultiPieceRows(candidates);
            }

            return unreceivedRowsByItem;
        }

        private void AddKnownValue(HashSet<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value.Trim());
            }
        }

        /// <summary>
        /// 未收單補列時，若一分號多件主分號沒有出現在同批未收單分號中，整筆資料不顯示。
        /// </summary>
        private List<AirMainUploadExcelRow> FilterInvalidMultiPieceRows(List<AirMainUploadExcelRow> uploadRows)
        {
            var rows = uploadRows ?? new List<AirMainUploadExcelRow>();
            var bagNos = new HashSet<string>(
                rows.Select(row => (row?.BagNo ?? "").Trim())
                    .Where(bagNo => !string.IsNullOrWhiteSpace(bagNo)),
                StringComparer.OrdinalIgnoreCase);

            if (!bagNos.Any())
            {
                return rows;
            }

            return rows.Where(row =>
            {
                var relatedValue = (row?.OneHwbMultiPieceHwb ?? "").Trim();
                if (string.IsNullOrWhiteSpace(relatedValue))
                {
                    return true;
                }

                var relatedBagNos = relatedValue
                    .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return !relatedBagNos.Any() || relatedBagNos.All(bagNos.Contains);
            }).ToList();
        }

        /// <summary>
        /// 取得指定主號已收單且仍未進倉的 ZZZA 上傳明細。
        /// </summary>
        private List<AirMainUploadExcelRow> GetZzzaReceivedRows(
            IAirMainComparisonItem item,
            Dictionary<string, List<AirMainUploadExcelRow>> uploadRowsByMwb)
        {
            List<AirMainUploadExcelRow> mainUploadRows;
            if (item == null ||
                uploadRowsByMwb == null ||
                !uploadRowsByMwb.TryGetValue((item.Mwb ?? "").Trim(), out mainUploadRows))
            {
                return new List<AirMainUploadExcelRow>();
            }

            var notGciValues = new HashSet<string>(
                (item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                    .SelectMany(row => new[] { row.Hwb, row.BagNo })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return mainUploadRows
                .Where(IsZzzaUploadRow)
                .Where(row => notGciValues.Contains((row.BagNo ?? "").Trim()))
                .ToList();
        }

        /// <summary>
        /// 取得指定主號已出現在查詢資料、且不在未進倉明細的 ZZZA 上傳明細。
        /// </summary>
        private List<AirMainUploadExcelRow> GetZzzaGciRows(
            IAirMainComparisonItem item,
            Dictionary<string, List<AirMainUploadExcelRow>> uploadRowsByMwb)
        {
            List<AirMainUploadExcelRow> mainUploadRows;
            if (item == null ||
                uploadRowsByMwb == null ||
                !uploadRowsByMwb.TryGetValue((item.Mwb ?? "").Trim(), out mainUploadRows))
            {
                return new List<AirMainUploadExcelRow>();
            }

            var queryValues = new HashSet<string>(
                (item.QueryRows ?? Enumerable.Empty<IAirMainQueryRow>())
                    .SelectMany(row => new[] { row.Hwb, row.ExpBagNo })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);
            var notGciValues = new HashSet<string>(
                (item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                    .SelectMany(row => new[] { row.Hwb, row.BagNo })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return mainUploadRows
                .Where(IsZzzaUploadRow)
                .Where(row => queryValues.Contains((row.BagNo ?? "").Trim()))
                .Where(row => !notGciValues.Contains((row.BagNo ?? "").Trim()))
                .ToList();
        }

        /// <summary>
        /// 取得未進倉明細對應的 ZZZA 顯示值。
        /// </summary>
        private string GetZzzaRemark(
            IAirMainDetailRow detail,
            IEnumerable<AirMainUploadExcelRow> zzzaReceivedRows)
        {
            if (detail == null)
            {
                return "";
            }

            var hwb = (detail.Hwb ?? "").Trim();
            var bagNo = (detail.BagNo ?? "").Trim();
            var hasZzza = (zzzaReceivedRows ?? Enumerable.Empty<AirMainUploadExcelRow>())
                .Where(IsZzzaUploadRow)
                .Select(row => (row.BagNo ?? "").Trim())
                .Any(value =>
                    (!string.IsNullOrWhiteSpace(hwb) && string.Equals(value, hwb, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(bagNo) && string.Equals(value, bagNo, StringComparison.OrdinalIgnoreCase)));

            return hasZzza ? ZzzaRemark : "";
        }

        /// <summary>
        /// 判斷上傳明細的備註是否為 ZZZA。
        /// </summary>
        private bool IsZzzaRemark(string remark)
        {
            return string.Equals((remark ?? "").Trim(), ZzzaRemark, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判斷上傳明細是否有 ZZZA 註記。
        /// </summary>
        private bool IsZzzaUploadRow(AirMainUploadExcelRow row)
        {
            return row != null && IsZzzaRemark(row.Remark);
        }

        /// <summary>
        /// 批次查詢未收單資料的派件公司。
        /// </summary>
        private void SetUnreceivedTransNames(IEnumerable<AirMainUploadExcelRow> uploadRows)
        {
            var rows = (uploadRows ?? Enumerable.Empty<AirMainUploadExcelRow>())
                .Where(row => !string.IsNullOrWhiteSpace(row.BagNo))
                .ToList();
            if (!rows.Any())
            {
                return;
            }

            var values = rows.Select(row => row.BagNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var lookup = GetOriginalTransNameLookup(values, true, true);

            foreach (var row in rows)
            {
                string transName;
                if (lookup.TryGetValue(row.BagNo.Trim(), out transName))
                {
                    row.TransName = transName;
                }
            }
        }

        /// <summary>
        /// 批次查詢未收單資料的錯單資料。
        /// 設定未收單指定錯單類別筆數。
        /// </summary>
        private void SetPlinkErrors(IEnumerable<AirMainUploadExcelRow> uploadRows)
        {
            var rows = (uploadRows ?? Enumerable.Empty<AirMainUploadExcelRow>()).ToList();
            var hawbs = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.BagNo))
                .Select(row => row.BagNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var lookup = GetPlinkErrorLookup(hawbs);
            foreach (var row in rows)
            {
                List<AirMainPlinkErrorRow> errors;
                row.PlinkErrors = !string.IsNullOrWhiteSpace(row.BagNo) &&
                    lookup.TryGetValue(row.BagNo.Trim(), out errors)
                    ? errors
                    : new List<AirMainPlinkErrorRow>();
            }
        }

        /// <summary>
        /// 取得袋號對應的錯單資料。
        /// </summary>
        private Dictionary<string, List<AirMainPlinkErrorRow>> GetPlinkErrorLookup(IEnumerable<string> hawbs)
        {
            var values = (hawbs ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (!values.Any())
            {
                return new Dictionary<string, List<AirMainPlinkErrorRow>>(StringComparer.OrdinalIgnoreCase);
            }

            return _dataCenterDb.EtlPlinkErrors
                .AsNoTracking()
                .WhereBulkContains(_dataCenterDb, values, row => row.Hawb, row => row)
                .Where(row => !string.IsNullOrWhiteSpace(row.Hawb))
                .OrderBy(row => row.Hawb)
                .ThenBy(row => row.RowId)
                .GroupBy(row => row.Hawb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(row => new AirMainPlinkErrorRow
                    {
                        Hawb = row.Hawb ?? "",
                        Reason = row.Reason ?? ""
                    }).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 批次查詢 AIR_DETAIN 狀態。
        /// </summary>
        private Dictionary<string, string> GetAirDetainStatusLookup(
            IEnumerable<IAirMainComparisonItem> results,
            IEnumerable<AirMainUploadExcelRow> unreceivedRows)
        {
            var trackingNos = (results ?? Enumerable.Empty<IAirMainComparisonItem>())
                .SelectMany(item => item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                .Select(detail => detail.Hwb)
                .Concat((unreceivedRows ?? Enumerable.Empty<AirMainUploadExcelRow>()).Select(row => row.BagNo))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (!trackingNos.Any())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return _dataCenterDb.AirDetains
                .AsNoTracking()
                .WhereBulkContains(_dataCenterDb, trackingNos, row => row.TrackingNo, row => row)
                .Where(row => !string.IsNullOrWhiteSpace(row.TrackingNo))
                .GroupBy(row => row.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => FormatAirDetainModel(group.Select(row => row.Model).FirstOrDefault()),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得 AIR_DETAIN 狀態顯示文字。
        /// </summary>
        private string GetAirDetainStatus(Dictionary<string, string> lookup, string hwb)
        {
            if (lookup == null || string.IsNullOrWhiteSpace(hwb))
            {
                return "";
            }

            string status;
            return lookup.TryGetValue(hwb.Trim(), out status) ? status : "";
        }

        /// <summary>
        /// 轉換 AIR_DETAIN MODEL 顯示文字。
        /// </summary>
        private string FormatAirDetainModel(string model)
        {
            if (model == "DU")
            {
                return "出口地扣留";
            }

            if (model == "GF")
            {
                return GTypeNoIdStatus;
            }

            return model ?? "";
        }

        /// <summary>
        /// 判斷錯單類別是否包含未收單統計指定的錯單類別。
        /// </summary>
        private bool ContainsB6FReason(string reason)
        {
            return !string.IsNullOrWhiteSpace(reason) &&
                (reason.IndexOf("B6F", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 reason.IndexOf("A03", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 reason.IndexOf("B6B", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 reason.IndexOf("B15", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 reason.IndexOf("B6C", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 計算排除 ZZZA 後的派件公司件數。
        /// </summary>
        private Dictionary<string, int> BuildTransNameCounts(
            IAirMainComparisonItem item,
            IEnumerable<AirMainUploadExcelRow> unreceivedRows)
        {
            var uploadRows = (unreceivedRows ?? Enumerable.Empty<AirMainUploadExcelRow>()).ToList();
            var transNames = (item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                .Where(row => string.IsNullOrEmpty(row.ZzzaRemark))
                .Select(row => NormalizeTransName(row.TransName))
                .Concat(uploadRows.Where(row => !IsZzzaUploadRow(row)).Select(row => NormalizeTransName(row.TransName)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return transNames.ToDictionary(
                transName => transName,
                transName => GetNotGciTransNameCount(item, transName) +
                    uploadRows.Count(row => !IsZzzaUploadRow(row) && IsSameTransName(row.TransName, transName)),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 計算未進倉明細中指定派件公司的申報件數。
        /// </summary>
        private int GetNotGciTransNameCount(IAirMainComparisonItem item, string transName)
        {
            // 派件公司欄位數量以明細頁「未進倉申報」為準；同報單號碼重複時只取第一筆。
            // 單筆申報大於 1 時，先扣除進倉件數，再進行派件公司加總。
            // 未收單沒有報單號碼，就使用分號「申報」= 1 計算。
            return (item?.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                .Where(row => string.IsNullOrEmpty(row.ZzzaRemark))
                .Where(row => IsSameTransName(row.TransName, transName))
                .Select(row => new
                {
                    Row = row,
                    Key = string.IsNullOrWhiteSpace(row.DeclNo) ? (row.Hwb ?? "").Trim() : row.DeclNo.Trim()
                })
                .GroupBy(value => value.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First().Row)
                .Sum(row => row.DeclaredPiece > 1 ? row.DeclaredPiece - row.GciPiece : row.DeclaredPiece);
        }

        /// <summary>
        /// 將件數大於 0 的派件公司組合成顯示文字。
        /// </summary>
        private string BuildTransNameSummary(Dictionary<string, int> transNameCounts)
        {
            return string.Concat((transNameCounts ?? new Dictionary<string, int>())
                .Where(item => item.Value > 0)
                .Select(item => $"{item.Key}共{item.Value}件"));
        }

        /// <summary>
        /// 比對派件公司名稱。
        /// </summary>
        private bool IsSameTransName(string source, string target)
        {
            return string.Equals(
                NormalizeTransName(source),
                NormalizeTransName(target),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得派件公司欄位名稱，查無派件公司時歸到固定欄位。
        /// </summary>
        private string NormalizeTransName(string transName)
        {
            return string.IsNullOrWhiteSpace(transName) ? NoTransName : transName.Trim();
        }

        /// <summary>
        /// 依主號整理主號2 頁籤的總件數。
        /// </summary>
        private Dictionary<string, string> BuildUploadTotalPieceByMwb(IEnumerable<AirMainUploadSummaryRow> uploadRows)
        {
            return (uploadRows ?? Enumerable.Empty<AirMainUploadSummaryRow>())
                .Where(row => !string.IsNullOrWhiteSpace(row.Mwb))
                .GroupBy(row => row.Mwb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(row => ParseUploadTransmissionTime(row.TransmissionTime))
                        .Select(row => row.TotalPiece ?? "")
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "",
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依主號整理主號2 頁籤的進口日期與航機班次。
        /// </summary>
        private Dictionary<string, AirMainUploadSummaryRow> BuildUploadSummaryByMwb(
            IEnumerable<AirMainUploadSummaryRow> uploadRows)
        {
            return (uploadRows ?? Enumerable.Empty<AirMainUploadSummaryRow>())
                .Where(row => !string.IsNullOrWhiteSpace(row.Mwb))
                .GroupBy(row => row.Mwb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(row => ParseUploadTransmissionTime(row.TransmissionTime)).First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 解析主號2 頁籤的傳輸時間。
        /// </summary>
        private DateTime ParseUploadTransmissionTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            var formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss", "yyyy/M/d H:mm:ss",
                "yyyy-MM-dd HH:mm", "yyyy/MM/dd HH:mm", "yyyy/M/d H:mm",
                "yyyy-MM-dd", "yyyy/MM/dd", "yyyy/M/d", "yyyyMMddHHmmss", "yyyyMMdd"
            };

            DateTime result;
            var trimmedValue = value.Trim();
            if (DateTime.TryParseExact(
                trimmedValue,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
            {
                return result;
            }

            return DateTime.TryParse(trimmedValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out result)
                ? result
                : DateTime.MinValue;
        }

        /// <summary>
        /// 建立第一個頁簽：主號查詢結果。
        /// </summary>
        private void CreateMainResultSheet(
            IWorkbook workbook,
            string sheetName,
            IList<IAirMainComparisonItem> results,
            Dictionary<string, string> uploadTotalPieceByMwb,
            Dictionary<string, AirMainUploadSummaryRow> uploadSummaryByMwb,
            ICellStyle headerStyle,
            ICellStyle dataStyle,
            ICellStyle numberStyle)
        {
            // ========== 第一個頁籤：主號查詢結果 ==========
            var sheet = workbook.CreateSheet(sheetName);
            // 建立表頭。
            var headers = new List<string>
            {
                "進口日期", "主號", "客戶名稱", "航班", "總袋數", "收單件數", "未進倉小計",
                "申報", "進倉", "未進倉件", "併袋", "進倉袋", "未進倉袋", "未收單件數",
                "未收單B6F", "G類無ID"
            };
            var transNames = results
                // 取得所有派件公司，加入表頭。
                .SelectMany(item => item.TransNameCounts?.Keys ?? Enumerable.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            headers.AddRange(transNames);
            headers.AddRange(new[] { "ZZZA", "ZZZA進倉", "ZZZA收單", "ZZZA未收單", "派件公司件數", "錯誤訊息" });

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);
            // 設定欄寬。
            for (int column = 0; column < headers.Count; column++)
            {
                sheet.SetColumnWidth(column, 5000);
            }

            // 填入資料。
            for (int index = 0; index < results.Count; index++)
            {
                var item = results[index];
                var row = sheet.CreateRow(index + 1);
                var column = 0;
                var mwb = (item.Mwb ?? "").Trim();

                AirMainUploadSummaryRow uploadSummary;
                uploadSummaryByMwb.TryGetValue(mwb, out uploadSummary);
                NpoiCell.CreateCell(row, column++, uploadSummary?.ImportDate ?? "", dataStyle);
                NpoiCell.CreateCell(row, column++, item.Mwb ?? "", dataStyle);
                NpoiCell.CreateCell(row, column++, item.Customer ?? "", dataStyle);
                NpoiCell.CreateCell(row, column++, uploadSummary?.FlightNumber ?? "", dataStyle);

                string uploadTotalPiece;
                var totalBagText = "";
                // 主號2 的「總件數」作為總袋數基準，扣除同主號的無派件公司統計數量。
                if (uploadTotalPieceByMwb.TryGetValue(mwb, out uploadTotalPiece))
                {
                    int totalBagCount;
                    if (int.TryParse(uploadTotalPiece, out totalBagCount))
                    {
                        int noTransNameCount;
                        (item.TransNameCounts ?? new Dictionary<string, int>())
                            .TryGetValue(NoTransName, out noTransNameCount);
                        totalBagText = (totalBagCount - noTransNameCount).ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // 非整數內容保留上傳檔原值，避免匯出時誤改資料。
                        totalBagText = uploadTotalPiece;
                    }
                }

                NpoiCell.CreateIntCell(row, column++, totalBagText, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.ReceivedPieceCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.NotGciTotal, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.DeclaredPiece, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.GciPiece, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.NotGciPiece, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.BagCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.GciBagCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.NotGciBag, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.UnreceivedCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.UnreceivedB6FCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.GTypeNoIdCount, numberStyle);

                // 派件公司統計：未進倉明細用「申報」加總，未收單補列每筆算 1。
                foreach (var transName in transNames)
                {
                    int count;
                    (item.TransNameCounts ?? new Dictionary<string, int>()).TryGetValue(transName, out count);
                    NpoiCell.CreateIntCell(row, column++, count, numberStyle);
                }

                NpoiCell.CreateIntCell(row, column++, item.ZzzaCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.ZzzaGciCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.ZzzaReceivedCount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.ZzzaUnreceivedCount, numberStyle);
                NpoiCell.CreateCell(row, column++, item.TransNameSummary ?? "", dataStyle);
                NpoiCell.CreateCell(row, column++, item.ErrorMessage ?? "", dataStyle);
            }
        }

        /// <summary>
        /// 建立第二個頁簽：未進倉明細。
        /// </summary>
        private void CreateNotGciDetailSheet(
            IWorkbook workbook,
            IEnumerable<IAirMainComparisonItem> results,
            ICellStyle headerStyle,
            ICellStyle dataStyle)
        {
            // ========== 第二個頁籤：未進倉明細 ==========
            var sheet = workbook.CreateSheet("未進倉明細");
            // 建立表頭。
            var headers = new[]
            {
                "項次", "提單號碼", "分號", "報單號碼", "袋號", "申報", "進倉", "出倉",
                "報關類別", "備註", "一分號多件", "錯單類別", "錯單單號", "派件公司", "狀態", "ZZZA"
            };
            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);
            // 設定欄寬。
            for (int column = 0; column < headers.Length; column++)
            {
                sheet.SetColumnWidth(column, 4000);
            }

            // 填入未進倉明細資料。
            // 填入明細資料。
            var rowIndex = 1;
            foreach (var item in results ?? Enumerable.Empty<IAirMainComparisonItem>())
            {
                var itemNo = 1;
                foreach (var detail in item.NotGciDetails ?? Enumerable.Empty<IAirMainDetailRow>())
                {
                    var row = sheet.CreateRow(rowIndex++);
                    NpoiCell.CreateIntCell(row, 0, itemNo++, dataStyle);
                    NpoiCell.CreateCell(row, 1, item.Mwb ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 2, detail.Hwb ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 3, detail.DeclNo ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 4, detail.BagNo ?? "", dataStyle);
                    NpoiCell.CreateIntCell(row, 5, detail.DeclaredPiece, dataStyle);
                    NpoiCell.CreateIntCell(row, 6, detail.GciPiece, dataStyle);
                    NpoiCell.CreateIntCell(row, 7, detail.GcoPiece, dataStyle);
                    NpoiCell.CreateCell(row, 8, detail.DeclType ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 9, detail.Remarks ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 10, "", dataStyle);
                    NpoiCell.CreateCell(row, 11, "", dataStyle);
                    NpoiCell.CreateCell(row, 12, "", dataStyle);
                    NpoiCell.CreateCell(row, 13, NormalizeTransName(detail.TransName), dataStyle);
                    NpoiCell.CreateCell(row, 14, detail.Status ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 15, detail.ZzzaRemark ?? "", dataStyle);
                }

                // 上傳檔有、查詢結果沒有時，依需求在未進倉明細補一列未收單資料。
                foreach (var uploadRow in item.UnreceivedRows ?? new List<AirMainUploadExcelRow>())
                {
                    var errorRows = uploadRow.PlinkErrors ?? new List<AirMainPlinkErrorRow>();
                    var errorReasons = string.Join(",",
                        errorRows
                            .Select(error => (error?.Reason ?? "").Trim())
                            .Where(reason => !string.IsNullOrWhiteSpace(reason))
                            .Distinct(StringComparer.OrdinalIgnoreCase));
                    var errorHawbs = string.Join(",",
                        errorRows
                            .Select(error => (error?.Hawb ?? "").Trim())
                            .Where(hawb => !string.IsNullOrWhiteSpace(hawb))
                            .Distinct(StringComparer.OrdinalIgnoreCase));

                    var row = sheet.CreateRow(rowIndex++);
                    NpoiCell.CreateIntCell(row, 0, itemNo++, dataStyle);
                    NpoiCell.CreateCell(row, 1, uploadRow.Mwb ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 2, uploadRow.BagNo ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 3, "未收單", dataStyle);
                    CreateBlankCells(row, 4, 9, dataStyle);
                    NpoiCell.CreateCell(row, 10, uploadRow.OneHwbMultiPieceHwb ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 11, errorReasons, dataStyle);
                    NpoiCell.CreateCell(row, 12, errorHawbs, dataStyle);
                    NpoiCell.CreateCell(row, 13, NormalizeTransName(uploadRow.TransName), dataStyle);
                    NpoiCell.CreateCell(row, 14, uploadRow.Status ?? "", dataStyle);
                    NpoiCell.CreateCell(row, 15, uploadRow.Remark ?? "", dataStyle);
                }
            }
        }

        /// <summary>
        /// 建立空白儲存格並套用樣式。
        /// </summary>
        private void CreateBlankCells(IRow row, int firstColumn, int lastColumn, ICellStyle style)
        {
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                NpoiCell.CreateCell(row, column, "", style);
            }
        }

    }
}
