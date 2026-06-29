using Dapper;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Extensions;
using Service.Models;
using Service.Services.Ftz.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Service.Services.Ftz
{
    public partial class FtzService
    {
        /// <summary>
        /// 主號查詢
        /// </summary>
        public async Task<ResponseModel> MainQueryAsync(FtzMainQueryRequest request, List<FtzMainUploadExcelRow> uploadRows = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Mwb))
                {
                    return new ResponseModel("請輸入主號");
                }

                // 解析主號列表（支援換行分隔）
                var mwbList = request.Mwb.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (!mwbList.Any())
                {
                    return new ResponseModel("請輸入主號");
                }

                var results = new List<FtzMainQueryViewModel>();

                using (var httpClient = GetHttpClient())
                {
                    foreach (var mwb in mwbList)
                    {
                        try
                        {
                            var result = await QuerySingleMainAsync(httpClient, mwb);
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            // 查詢失敗時，建立錯誤記錄
                            results.Add(new FtzMainQueryViewModel
                            {
                                Mwb = mwb,
                                HwbCount = "0",
                                HwbPiece = "0",
                                HwbGciPiece = "0",
                                HwbGcoPiece = "0",
                                GciWeight = "0",
                                NotGciPiece = 0,
                                ExpBagCount = "0",
                                ExpBagGciCount = "0",
                                ExpBagGcoCount = "0",
                                NotGciBag = 0,
                                NotGciTotal = 0,
                                ErrorMessage = $"查詢失敗：{ex.Message}"
                            });
                        }
                    }
                }

                //取得派件公司
                GetTransName(results);
                SetUnreceivedB6FCounts(results, uploadRows);

                return new ResponseModel
                {
                    ReturnObject = results
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"查詢錯誤：{ex.Message}");
            }
        }

        //取得派件公司
        private void GetTransName(List<FtzMainQueryViewModel> list)
        {
            // 收集所有需要查詢的主號、分號、袋號
            var allMwbs = list.Select(r => r.Mwb).Distinct().ToList();
            var allHwbs = list.SelectMany(r => r.NotGciDetails ?? new List<Row>())
                             .Where(x => string.IsNullOrEmpty(x.expBagNo))
                             .Select(x => x.hwb)
                             .Distinct()
                             .ToList();
            var allBagNos = list.SelectMany(r => r.NotGciDetails ?? new List<Row>())
                               .Where(x => !string.IsNullOrEmpty(x.expBagNo))
                               .Select(x => x.expBagNo)
                               .Distinct()
                               .ToList();

            // 建立字典存放查詢結果
            var customerDict = new Dictionary<string, string>();
            var flightDict = new Dictionary<string, string>();
            var trackingNoDict = new Dictionary<string, string>();
            var bagNoDict = new Dictionary<string, string>();
            Dictionary<string, string> airTransNameLookup = null;

            // 1. 批次查詢客戶名稱
            if (allMwbs.Any())
            {
                var sql = @"
                        SELECT a.MAINNUMBER, b.DESPATCHNAME 
                        FROM [DATA_CENTER].[dbo].[MAINORDERINFO] a 
                        JOIN [DATA_CENTER].[dbo].[DESPATCHFROM] b ON a.DELIVERYFROM = b.DESPATCHNO 
                        WHERE a.MAINNUMBER IN @Mwbs 
                        GROUP BY a.MAINNUMBER, b.DESPATCHNAME";

                customerDict = conn.Query<(string MAINNUMBER, string DESPATCHNAME)>(sql, new { Mwbs = allMwbs })
                    .ToDictionary(r => r.MAINNUMBER,r=> r.DESPATCHNAME);
            }

            // 2. 批次查詢班機
            if (allMwbs.Any())
            {
                var sql = @"
                        SELECT MAINNUMBER, FLIGHTNUMBER 
                        FROM [DATA_CENTER].[dbo].[MAINORDERINFO] 
                        WHERE MAINNUMBER IN @Mwbs";

                var flights = conn.Query<(string MAINNUMBER, string FLIGHTNUMBER)>(sql, new { Mwbs = allMwbs });
                
                flightDict = flights
                     .GroupBy(x => x.MAINNUMBER)
                     .ToDictionary(
                         g => g.Key,
                         g => g.First().FLIGHTNUMBER
                     );
            }

            // 3. 批次查詢分提單號的派件公司
            if (allHwbs.Any())
            {
                airTransNameLookup = GetAirTransNameLookup();
                trackingNoDict = GetOriginalTransNameLookup(allHwbs, true, true, airTransNameLookup);
            }

            // 4. 批次查詢袋號的派件公司
            if (allBagNos.Any())
            {
                if (airTransNameLookup == null)
                {
                    airTransNameLookup = GetAirTransNameLookup();
                }

                bagNoDict = GetOriginalTransNameLookup(allBagNos, false, true, airTransNameLookup);
            }

            // 使用字典填充資料
            list.ForEach(r =>
            {
                // 設定客戶名稱
                if (customerDict.ContainsKey(r.Mwb))
                {
                    r.Customer = customerDict[r.Mwb];
                }

                // 設定班機
                if (flightDict.ContainsKey(r.Mwb))
                {
                    r.FlightNumber = flightDict[r.Mwb];
                }

                // 設定派件公司
                r.NotGciDetails?.ForEach(x =>
                {
                    if (string.IsNullOrEmpty(x.expBagNo))
                    {
                        // 用分提單號查詢
                        if (trackingNoDict.ContainsKey(x.hwb))
                        {
                            x.TransName = trackingNoDict[x.hwb];
                        }
                    }
                    else
                    {
                        // 用袋號查詢
                        if (bagNoDict.ContainsKey(x.expBagNo))
                        {
                            x.TransName = bagNoDict[x.expBagNo];
                        }
                    }
                });
            });
        }

        /// <summary>
        /// 取得空運派件代碼與派件公司名稱對照。
        /// </summary>
        private Dictionary<string, string> GetAirTransNameLookup()
        {
            var sql = @"
                    SELECT TRANS_NO, TRANS_NAME
                    FROM [jetf].[dbo].[customer_master]
                    WHERE TRAN_TYPE = N'空運'";

            return conn.Query<(string TRANS_NO, string TRANS_NAME)>(sql)
                .Where(x => !string.IsNullOrWhiteSpace(x.TRANS_NO))
                .GroupBy(x => x.TRANS_NO.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.TRANS_NAME).FirstOrDefault(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依分號或袋號批次查詢派件公司名稱。
        /// </summary>
        private Dictionary<string, string> GetOriginalTransNameLookup(
            IEnumerable<string> trackingNos,
            bool includeTrackingNo,
            bool includeBagNo,
            Dictionary<string, string> transNameLookup = null)
        {
            var keys = (trackingNos ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!keys.Any() || (!includeTrackingNo && !includeBagNo))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var originalRows = new List<(string TrackingNo, string TransNo)>();
            if (includeTrackingNo)
            {
                originalRows.AddRange(DataCenterDb.OriginalLists
                    .AsNoTracking()
                    .WhereBulkContains(DataCenterDb, keys, x => x.TrackingNo, x => x)
                    .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                    .Select(x => (
                        x.TrackingNo,
                        x.ClearanceWarehousing.HasValue ? x.ClearanceWarehousing.Value.ToString() : "")));
            }

            if (includeBagNo)
            {
                originalRows.AddRange(DataCenterDb.OriginalLists
                    .AsNoTracking()
                    .WhereBulkContains(DataCenterDb, keys, x => x.BagNo, x => x)
                    .Where(x => !string.IsNullOrWhiteSpace(x.BagNo))
                    .Select(x => (
                        x.BagNo,
                        x.ClearanceWarehousing.HasValue ? x.ClearanceWarehousing.Value.ToString() : "")));
            }

            transNameLookup = transNameLookup ?? GetAirTransNameLookup();

            return originalRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => GetTransNameByTransNo(transNameLookup, x.Select(y => y.TransNo).FirstOrDefault()),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依派件代碼取得派件公司名稱。
        /// </summary>
        private string GetTransNameByTransNo(Dictionary<string, string> transNameLookup, string transNo)
        {
            if (string.IsNullOrWhiteSpace(transNo))
            {
                return "";
            }

            string transName;
            var key = transNo.Trim();
            return transNameLookup != null && transNameLookup.TryGetValue(key, out transName)
                ? transName
                : "";
        }

        /// <summary>
        /// 查詢單筆主號資料
        /// </summary>
        private async Task<FtzMainQueryViewModel> QuerySingleMainAsync(HttpClient httpClient, string mwb)
        {
            // 建立查詢 URL

            var queryUrl = $"{MAIN_QUERY_URL}?ieType=I&mwb={Uri.EscapeDataString(mwb)}&eid=0335&boxno=0H4&_search=false&nd={DateTimeOffset.Now.ToUnixTimeMilliseconds()}&rows=10000&page=1&sidx=&sord=asc";

            var response = await httpClient.GetAsync(queryUrl);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new Exception("無回應資料");
            }

            // 解析 JSON 回應
            var mainQueryResult = JsonConvert.DeserializeObject<FtzMainQueryResult>(jsonContent);

            if (mainQueryResult?.userdata == null)
            {
                throw new Exception("無法解析資料");
            }

            // 轉換為 ViewModel 並計算欄位
            var model = ConvertToViewModel(mainQueryResult);
            model.Mwb = mwb; // 設定主號

            //查詢申報未入倉明細
            if (model.NotGciTotal > 0)
            {
                model.NotGciDetails = await QueryNotGciDetailsAsync(httpClient, mwb);

                //未進倉申報袋號
                model.NotGciPieceExpBagNo = string.Join(",", model.NotGciDetails
                    .Where(r => !string.IsNullOrEmpty(r.expBagNo) && !(r.declNo ?? "").Contains("0H4W"))
                    .Select(r => r.expBagNo)
                    .Distinct());
            }

            return model;
        }

        /// <summary>
        /// 將 API 回應轉換為 ViewModel 並計算相關欄位
        /// </summary>
        private FtzMainQueryViewModel ConvertToViewModel(FtzMainQueryResult rawData)
        {
            var userData = rawData.userdata;

            // 解析數值（安全轉換）
            int hwbPiece = ParseInt(userData.hwbPiece);
            int hwbGciPiece = ParseInt(userData.hwbGciPiece);
            int expBagCount = ParseInt(userData.expBagCount);
            int expBagGciCount = ParseInt(userData.expBagGciCount);

            // 計算未進倉 = 申報 - 進倉
            int notGciPiece = hwbPiece - hwbGciPiece;

            // 計算未進倉袋 = 併袋 - 進倉袋
            int notGciBag = expBagCount - expBagGciCount;

            // 計算未進倉小計 = 未進倉 + 未進倉袋
            int notGciTotal = notGciPiece + notGciBag;

            return new FtzMainQueryViewModel
            {
                HwbCount = userData.hwbCount,
                HwbPiece = userData.hwbPiece,
                HwbGciPiece = userData.hwbGciPiece,
                HwbGcoPiece = userData.hwbGcoPiece,
                GciWeight = userData.gciWeight ?? userData.hwbGciWt,
                NotGciPiece = notGciPiece,
                ExpBagCount = userData.expBagCount,
                ExpBagGciCount = userData.expBagGciCount,
                ExpBagGcoCount = userData.expBagGcoCount,
                NotGciBag = notGciBag,
                NotGciTotal = notGciTotal,
                RawData = rawData
            };
        }

        /// <summary>
        /// 安全解析整數
        /// </summary>
        private int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            if (int.TryParse(value, out int result))
            {
                return result;
            }

            return 0;
        }

        /// <summary>
        /// 查詢申報未入倉明細
        /// </summary>
        private async Task<List<Row>> QueryNotGciDetailsAsync(HttpClient httpClient, string mwb)
        {
            try
            {
                // 取得當前時間
                var now = DateTime.Now;
                var d1Date = now.AddDays(-1); // now - 1天

                // 格式化參數
                string d1 = d1Date.ToString("yyyyMMdd");
                string t1 = d1Date.ToString("HHmm");
                string d2 = now.ToString("yyyyMMdd");
                string t2 = now.ToString("HHmm");
                string nd = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

                // 建立查詢 URL
                var queryUrl = $"{NOGCI_QUERY_URL}?ieType=I&eid=0335&d1={d1}&t1={t1}&d2={d2}&t2={t2}&mwb={Uri.EscapeDataString(mwb)}&_search=false&nd={nd}&rows=10000&page=1&sidx=&sord=asc";

                // 發送請求
                var response = await httpClient.GetAsync(queryUrl);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<Row>();
                }

                // 解析 JSON 回應
                var result = JsonConvert.DeserializeObject<FtzNoGciQueryResult>(jsonContent);

                if (result?.rows == null)
                {
                    return new List<Row>();
                }

                return result.rows;
            }
            catch (Exception)
            {
                // 查詢失敗時返回空列表
                return new List<Row>();
            }
        }

        /// <summary>
        /// 讀取主號查詢上傳 Excel 的明細資料。
        /// </summary>
        /// <param name="uploadStream">上傳檔案串流。</param>
        /// <returns>分艙單收單註記為 X 的資料列。</returns>
        public List<FtzMainUploadExcelRow> ReadMainUploadRows(Stream uploadStream)
        {
            if (uploadStream == null)
            {
                return new List<FtzMainUploadExcelRow>();
            }

            // 同一個檔案可能先被查詢流程讀過，再被匯出流程重讀，先把串流位置歸零。
            if (uploadStream.CanSeek)
            {
                uploadStream.Position = 0;
            }

            IWorkbook workbook;
            try
            {
                workbook = WorkbookFactory.Create(uploadStream);
            }
            catch (Exception ex)
            {
                throw new Exception($"讀取 Excel 失敗：{ex.Message}");
            }

            // 需求只接受「明細」頁籤，其他頁籤資料一律不處理。
            var sheet = workbook.GetSheet("明細");
            if (sheet == null)
            {
                throw new Exception("找不到 Excel 頁籤：明細");
            }

            // 先定位表頭，之後才能依欄名讀取主號、袋號與收單註記。
            var headerInfo = FindMainUploadHeader(sheet);
            var headerMap = headerInfo.Item2;
            var requiredHeaders = new[] { "袋號", "主號", "分艙單收單註記", "1分號多件之分號" };
            var missingHeaders = requiredHeaders.Where(header => !headerMap.ContainsKey(header)).ToList();

            if (missingHeaders.Any())
            {
                throw new Exception($"明細頁籤缺少欄位：{string.Join("、", missingHeaders)}");
            }

            var uploadRows = new List<FtzMainUploadExcelRow>();
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

                // 只有收單註記為 X 的資料，才需要納入後續未收單比對。
                if (!string.Equals(receiptMark, "X", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 主號或袋號缺值時無法比對，直接略過。
                if (string.IsNullOrWhiteSpace(bagNo) || string.IsNullOrWhiteSpace(mwb))
                {
                    continue;
                }

                uploadRows.Add(new FtzMainUploadExcelRow
                {
                    BagNo = bagNo.Trim(),
                    Mwb = mwb.Trim(),
                    ReceiptMark = receiptMark.Trim(),
                    OneHwbMultiPieceHwb = (oneHwbMultiPieceHwb ?? "").Trim()
                });
            }

            return uploadRows;
        }

        /// <summary>
        /// 尋找主號上傳 Excel 的表頭列。
        /// </summary>
        /// <param name="sheet">工作表。</param>
        /// <returns>表頭列索引與欄位對照。</returns>
        private Tuple<int, Dictionary<string, int>> FindMainUploadHeader(ISheet sheet)
        {
            var requiredHeaders = new[] { "袋號", "主號", "分艙單收單註記", "1分號多件之分號" };
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
        /// 依主號整理主號查詢上傳資料。
        /// </summary>
        private Dictionary<string, List<FtzMainUploadExcelRow>> BuildMainUploadRowsByMwb(IEnumerable<FtzMainUploadExcelRow> uploadRows)
        {
            return (uploadRows ?? Enumerable.Empty<FtzMainUploadExcelRow>())
                .Where(row => !string.IsNullOrWhiteSpace(row.Mwb) && !string.IsNullOrWhiteSpace(row.BagNo))
                .GroupBy(row => row.Mwb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(row => row.BagNo.Trim(), StringComparer.OrdinalIgnoreCase)
                        .Select(rowGroup => rowGroup.First())
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 設定未收單 B6F 筆數。
        /// </summary>
        private void SetUnreceivedB6FCounts(List<FtzMainQueryViewModel> results, List<FtzMainUploadExcelRow> uploadRows)
        {
            if (results == null || uploadRows == null || !uploadRows.Any())
            {
                return;
            }

            var uploadRowsByMwb = BuildMainUploadRowsByMwb(uploadRows);
            var unreceivedRowsByMainItem = GetUnreceivedUploadRowsByMainItem(results, uploadRowsByMwb);
            var unreceivedRows = unreceivedRowsByMainItem.SelectMany(x => x.Value).ToList();
            var plinkErrorRowsLookup = GetPlinkErrorRowsLookup(unreceivedRows);

            SetUnreceivedB6FCounts(results, unreceivedRowsByMainItem, plinkErrorRowsLookup);
        }

        /// <summary>
        /// 設定未收單 B6F 筆數。
        /// </summary>
        private void SetUnreceivedB6FCounts(
            List<FtzMainQueryViewModel> results,
            Dictionary<FtzMainQueryViewModel, List<FtzMainUploadExcelRow>> unreceivedRowsByMainItem,
            Dictionary<string, List<FtzPlinkErrorRow>> plinkErrorRowsLookup)
        {
            foreach (var result in results ?? new List<FtzMainQueryViewModel>())
            {
                List<FtzMainUploadExcelRow> unreceivedRows;
                if (unreceivedRowsByMainItem == null ||
                    !unreceivedRowsByMainItem.TryGetValue(result, out unreceivedRows))
                {
                    result.UnreceivedB6FCount = 0;
                    continue;
                }

                result.UnreceivedB6FCount = unreceivedRows
                    .Count(row => GetPlinkErrorRows(plinkErrorRowsLookup, row.BagNo)
                        .Any(x => ContainsB6FReason(x.Reason)));
            }
        }

        /// <summary>
        /// 主號查詢匯出 Excel
        /// </summary>
        public async Task<IWorkbook> ExportMainExcel(FtzMainQueryRequest request, List<FtzMainUploadExcelRow> uploadRows = null)
        {
            // 先查詢資料
            var queryResult = await MainQueryAsync(request);

            if (queryResult.status != Status.success || queryResult.ReturnObject == null)
            {
                throw new Exception(queryResult.msg ?? "查詢失敗");
            }

            var results = queryResult.ReturnObject as List<FtzMainQueryViewModel> ?? new List<FtzMainQueryViewModel>();

            // 建立 Excel
            IWorkbook workbook = new XSSFWorkbook();

            // 建立樣式
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 12, true);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);

            // 先把上傳資料整理成實際需要補到未進倉明細的未收單資料，供兩個頁籤共用。
            var uploadRowsByMwb = BuildMainUploadRowsByMwb(uploadRows);

            var unreceivedUploadRowsByMainItem = GetUnreceivedUploadRowsByMainItem(results, uploadRowsByMwb);
            var unreceivedUploadRows = unreceivedUploadRowsByMainItem.SelectMany(x => x.Value).ToList();
            SetUnreceivedTransName(unreceivedUploadRows);
            var plinkErrorRowsLookup = GetPlinkErrorRowsLookup(unreceivedUploadRows);
            SetUnreceivedB6FCounts(results, unreceivedUploadRowsByMainItem, plinkErrorRowsLookup);
            var airDetainStatusLookup = GetAirDetainStatusLookup(
                results,
                unreceivedUploadRows);

            // ========== 第一個頁籤：主號查詢結果 ==========
            ISheet sheet = workbook.CreateSheet("Ftz主號查詢結果");

            // 建立表頭
            var headers = new List<string>
              {
                  "主號","客戶名稱","航班", "申報", "進倉","未進倉件","未收單B6F",
                                    "併袋", "進倉袋", "未進倉袋", "未進倉小計"
              };

            //取得所有派件公司，加入表頭
            var transNames = results.Where(r => r.NotGciDetails != null)
                            .SelectMany(r => r.NotGciDetails)
                            .Where(r => r.TransName != null)
                            .Select(r => r.TransName)
                            .Distinct()
                            .ToList();

            headers.AddRange(transNames);
            headers.Add("未收單件數");

            var unreceivedTransNames = unreceivedUploadRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TransName))
                .Select(x => x.TransName)
                .Distinct()
                .ToList();
            headers.AddRange(unreceivedTransNames.Select(x => x));
            headers.Add("錯誤訊息");

            IRow headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            // 設定欄寬
            for (int i = 0; i < headers.Count; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }

            // 填入資料
            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                IRow dataRow = sheet.CreateRow(i + 1);
                var column = 0;
                NpoiCell.CreateCell(dataRow, column++, item.Mwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.Customer ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.FlightNumber ?? "", dataStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.HwbPiece ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.HwbGciPiece ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.NotGciPiece, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.UnreceivedB6FCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ExpBagCount ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ExpBagGciCount ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.NotGciBag, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.NotGciTotal, numberStyle);

                //派件公司
                foreach (var transName in transNames)
                {
                    //一分號多件
                    var bagnosCount = item.NotGciDetails?.Where(r => !string.IsNullOrEmpty(r.realTotBag) && string.IsNullOrEmpty(r.expBagNo) && r.TransName == transName).Select(r => r.realTotBagCount).Sum() ?? 0;
                    //件數
                    var trackingnoCount = item.NotGciDetails?.Where(r => string.IsNullOrEmpty(r.realTotBag) && string.IsNullOrEmpty(r.expBagNo) && r.TransName == transName).Count() ?? 0;
                    //袋數
                    var bagnoCount = item.NotGciDetails?.Where(r => !string.IsNullOrEmpty(r.expBagNo) && r.TransName == transName)
                                              .Select(r => new
                                              {
                                                  r.expBagNo,
                                                  r.TransName
                                              }).Distinct().Count() ?? 0;

                    var totalCount = bagnosCount + trackingnoCount + bagnoCount;

                    NpoiCell.CreateIntCell(dataRow, column++, totalCount, numberStyle);
                }

                List<FtzMainUploadExcelRow> unreceivedRows;
                var unreceivedCount = unreceivedUploadRowsByMainItem.TryGetValue(item, out unreceivedRows)
                    ? unreceivedRows.Count
                    : 0;
                NpoiCell.CreateIntCell(dataRow, column++, unreceivedCount, numberStyle);

                foreach (var transName in unreceivedTransNames)
                {
                    var unreceivedTransNameCount = unreceivedRows?.Count(x => x.TransName == transName) ?? 0;
                    NpoiCell.CreateIntCell(dataRow, column++, unreceivedTransNameCount, numberStyle);
                }

                NpoiCell.CreateCell(dataRow, column++, item.ErrorMessage ?? "", dataStyle);
            }

            // ========== 第二個頁籤：未進倉明細 ==========
            ISheet detailSheet = workbook.CreateSheet("未進倉明細");

            // 建立表頭
            string[] detailHeaders = new string[]
            {
                "項次", "提單號碼", "分號", "報單號碼", "袋號",
                "申報", "進倉", "出倉", "報關類別", "備註", "一分號多件", "錯單類別", "錯單單號", "派件公司", "狀態"
            };

            IRow detailHeaderRow = detailSheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(detailHeaderRow, detailHeaders, headerStyle);

            // 設定欄寬
            for (int i = 0; i < detailHeaders.Length; i++)
            {
                detailSheet.SetColumnWidth(i, 4000);
            }

            // 填入未進倉明細資料
            var detailRowIndex = 1;
            foreach (var mainItem in results)
            {
                var itemNo = 1;
                if (mainItem.NotGciDetails != null && mainItem.NotGciDetails.Any())
                {
                    foreach (var detail in mainItem.NotGciDetails)
                    {
                        IRow detailDataRow = detailSheet.CreateRow(detailRowIndex);

                        NpoiCell.CreateIntCell(detailDataRow, 0, itemNo++, dataStyle); // 項次
                        NpoiCell.CreateCell(detailDataRow, 1, detail.mwb ?? "", dataStyle); // 提單號碼
                        NpoiCell.CreateCell(detailDataRow, 2, detail.hwb ?? "", dataStyle); // 分號
                        NpoiCell.CreateCell(detailDataRow, 3, detail.declNo ?? "", dataStyle); // 報單號碼
                        NpoiCell.CreateCell(detailDataRow, 4, detail.expBagNo ?? "", dataStyle); // 袋號
                        NpoiCell.CreateCell(detailDataRow, 5, detail.piece ?? "", dataStyle); // 申報
                        NpoiCell.CreateCell(detailDataRow, 6, detail.gciPiece ?? "", dataStyle); // 進倉
                        NpoiCell.CreateCell(detailDataRow, 7, detail.gcoPiece ?? "", dataStyle); // 出倉
                        NpoiCell.CreateCell(detailDataRow, 8, detail.declType ?? "", dataStyle); // 報關類別
                        NpoiCell.CreateCell(detailDataRow, 9, detail.remarks ?? "", dataStyle); // 備註
                        NpoiCell.CreateCell(detailDataRow, 10, "", dataStyle); // 一分號多件
                        NpoiCell.CreateCell(detailDataRow, 11, "", dataStyle); // 錯單類別
                        NpoiCell.CreateCell(detailDataRow, 12, "", dataStyle); // 錯單單號
                        NpoiCell.CreateCell(detailDataRow, 13, detail.TransName ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 14, GetAirDetainStatus(airDetainStatusLookup, detail.hwb), dataStyle);
                        detailRowIndex++;
                    }
                }

                List<FtzMainUploadExcelRow> mainUploadRows;
                if (!unreceivedUploadRowsByMainItem.TryGetValue(mainItem, out mainUploadRows))
                {
                    continue;
                }

                foreach (var uploadRow in mainUploadRows)
                {
                    // 上傳檔有、查詢結果沒有時，依需求在未進倉明細補一列未收單資料。
                    var errorRows = GetPlinkErrorRows(plinkErrorRowsLookup, uploadRow.BagNo);
                    var errorRowCount = Math.Max(errorRows.Count, 1);
                    var startRowIndex = detailRowIndex;
                    var status = GetAirDetainStatus(airDetainStatusLookup, uploadRow.BagNo);
                    var displayItemNo = itemNo++;

                    for (int errorIndex = 0; errorIndex < errorRowCount; errorIndex++)
                    {
                        IRow detailDataRow = detailSheet.CreateRow(detailRowIndex);

                        if (errorIndex == 0)
                        {
                            NpoiCell.CreateIntCell(detailDataRow, 0, displayItemNo, dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 1, uploadRow.Mwb ?? "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 2, uploadRow.BagNo ?? "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 3, "未收單", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 4, "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 5, "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 6, "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 7, "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 8, "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 9, "", dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 10, uploadRow.OneHwbMultiPieceHwb ?? "", dataStyle);
                        }
                        else
                        {
                            CreateBlankCells(detailDataRow, 0, 10, dataStyle);
                        }

                        var errorRow = errorIndex < errorRows.Count ? errorRows[errorIndex] : null;
                        NpoiCell.CreateCell(detailDataRow, 11, errorRow?.Reason ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 12, errorRow?.Hawb ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 13, uploadRow.TransName ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 14, status, dataStyle);
                        detailRowIndex++;
                    }

                    MergeColumns(detailSheet, startRowIndex, detailRowIndex - 1, 0, 10);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 取得需要補到未進倉明細的未收單上傳資料。
        /// </summary>
        private Dictionary<FtzMainQueryViewModel, List<FtzMainUploadExcelRow>> GetUnreceivedUploadRowsByMainItem(
            List<FtzMainQueryViewModel> results,
            Dictionary<string, List<FtzMainUploadExcelRow>> uploadRowsByMwb)
        {
            var unreceivedRowsByMainItem = new Dictionary<FtzMainQueryViewModel, List<FtzMainUploadExcelRow>>();
            foreach (var mainItem in results ?? new List<FtzMainQueryViewModel>())
            {
                var mwb = string.IsNullOrWhiteSpace(mainItem.Mwb) ? string.Empty : mainItem.Mwb.Trim();
                List<FtzMainUploadExcelRow> mainUploadRows;
                if (!string.IsNullOrWhiteSpace(mainItem.ErrorMessage) ||
                    uploadRowsByMwb == null ||
                    !uploadRowsByMwb.TryGetValue(mwb, out mainUploadRows))
                {
                    continue;
                }

                var knownHwbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (mainItem.NotGciDetails != null)
                {
                    foreach (var detail in mainItem.NotGciDetails)
                    {
                        var hwb = string.IsNullOrWhiteSpace(detail.hwb) ? string.Empty : detail.hwb.Trim();
                        if (string.IsNullOrWhiteSpace(hwb))
                        {
                            continue;
                        }

                        knownHwbs.Add(hwb);
                    }
                }

                if (mainItem.RawData?.Rows != null)
                {
                    foreach (var rawRow in mainItem.RawData.Rows)
                    {
                        var rawHwb = string.IsNullOrWhiteSpace(rawRow.Hwb) ? string.Empty : rawRow.Hwb.Trim();
                        if (!string.IsNullOrWhiteSpace(rawHwb))
                        {
                            knownHwbs.Add(rawHwb);
                        }
                    }
                }

                var candidateUnreceivedRows = mainUploadRows
                    .Where(uploadRow =>
                    {
                        var bagNo = string.IsNullOrWhiteSpace(uploadRow.BagNo) ? string.Empty : uploadRow.BagNo.Trim();
                        return !string.IsNullOrWhiteSpace(bagNo) && !knownHwbs.Contains(bagNo);
                    })
                    .ToList();

                unreceivedRowsByMainItem[mainItem] = FilterInvalidUnreceivedMultiPieceRows(candidateUnreceivedRows);
            }

            return unreceivedRowsByMainItem;
        }

        /// <summary>
        /// 未收單補列時，若一分號多件主分號沒有出現在同批未收單分號中，整筆資料不顯示。
        /// </summary>
        private List<FtzMainUploadExcelRow> FilterInvalidUnreceivedMultiPieceRows(List<FtzMainUploadExcelRow> uploadRows)
        {
            var rows = uploadRows ?? new List<FtzMainUploadExcelRow>();
            if (!rows.Any())
            {
                return rows;
            }

            var bagNos = new HashSet<string>(
                rows
                    .Select(row => string.IsNullOrWhiteSpace(row?.BagNo) ? string.Empty : row.BagNo.Trim())
                    .Where(bagNo => !string.IsNullOrWhiteSpace(bagNo)),
                StringComparer.OrdinalIgnoreCase);

            if (!bagNos.Any())
            {
                return rows;
            }

            return rows
                .Where(row =>
                {
                    var oneHwbMultiPieceHwb = string.IsNullOrWhiteSpace(row?.OneHwbMultiPieceHwb)
                        ? string.Empty
                        : row.OneHwbMultiPieceHwb.Trim();

                    if (string.IsNullOrWhiteSpace(oneHwbMultiPieceHwb))
                    {
                        return true;
                    }

                    var relatedBagNos = oneHwbMultiPieceHwb
                        .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(bagNo => bagNo.Trim())
                        .Where(bagNo => !string.IsNullOrWhiteSpace(bagNo))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return !relatedBagNos.Any() || relatedBagNos.All(bagNos.Contains);
                })
                .ToList();
        }

        /// <summary>
        /// 批次查詢未收單資料的派件公司。
        /// </summary>
        private void SetUnreceivedTransName(IEnumerable<FtzMainUploadExcelRow> uploadRows)
        {
            var rows = (uploadRows ?? Enumerable.Empty<FtzMainUploadExcelRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.BagNo))
                .ToList();

            if (!rows.Any())
            {
                return;
            }

            var trackingNos = rows
                .Select(x => x.BagNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var transNameLookup = GetOriginalTransNameLookup(trackingNos, true, true);

            foreach (var row in rows)
            {
                string transName;
                if (transNameLookup.TryGetValue(row.BagNo.Trim(), out transName))
                {
                    row.TransName = transName;
                }
            }
        }

        /// <summary>
        /// 批次查詢未收單資料的錯單資料。
        /// </summary>
        private Dictionary<string, List<FtzPlinkErrorRow>> GetPlinkErrorRowsLookup(IEnumerable<FtzMainUploadExcelRow> uploadRows)
        {
            var hawbs = (uploadRows ?? Enumerable.Empty<FtzMainUploadExcelRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.BagNo))
                .Select(x => x.BagNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!hawbs.Any())
            {
                return new Dictionary<string, List<FtzPlinkErrorRow>>(StringComparer.OrdinalIgnoreCase);
            }

            return DataCenterDb.EtlPlinkErrors
                .AsNoTracking()
                .WhereBulkContains(DataCenterDb, hawbs, x => x.Hawb, x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x.Hawb))
                .OrderBy(x => x.Hawb)
                .ThenBy(x => x.RowId)
                .GroupBy(x => x.Hawb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => new FtzPlinkErrorRow
                    {
                        Hawb = y.Hawb ?? "",
                        Reason = y.Reason ?? ""
                    }).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得袋號對應的錯單資料。
        /// </summary>
        private List<FtzPlinkErrorRow> GetPlinkErrorRows(Dictionary<string, List<FtzPlinkErrorRow>> lookup, string bagNo)
        {
            if (lookup == null || string.IsNullOrWhiteSpace(bagNo))
            {
                return new List<FtzPlinkErrorRow>();
            }

            List<FtzPlinkErrorRow> rows;
            return lookup.TryGetValue(bagNo.Trim(), out rows)
                ? rows
                : new List<FtzPlinkErrorRow>();
        }

        /// <summary>
        /// 判斷錯單類別是否包含 B6F。
        /// </summary>
        private bool ContainsB6FReason(string reason)
        {
            return !string.IsNullOrWhiteSpace(reason) &&
                reason.IndexOf("B6F", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 建立空白儲存格並套用樣式。
        /// </summary>
        private void CreateBlankCells(IRow row, int firstColumn, int lastColumn, ICellStyle style)
        {
            for (int columnIndex = firstColumn; columnIndex <= lastColumn; columnIndex++)
            {
                NpoiCell.CreateCell(row, columnIndex, "", style);
            }
        }

        /// <summary>
        /// 合併指定欄位的多列儲存格。
        /// </summary>
        private void MergeColumns(ISheet sheet, int firstRow, int lastRow, int firstColumn, int lastColumn)
        {
            if (sheet == null || lastRow <= firstRow)
            {
                return;
            }

            for (int columnIndex = firstColumn; columnIndex <= lastColumn; columnIndex++)
            {
                sheet.AddMergedRegion(new CellRangeAddress(firstRow, lastRow, columnIndex, columnIndex));
            }
        }

        /// <summary>
        /// 批次查詢 AIR_DETAIN 狀態。
        /// </summary>
        private Dictionary<string, string> GetAirDetainStatusLookup(
            List<FtzMainQueryViewModel> results,
            IEnumerable<FtzMainUploadExcelRow> unreceivedUploadRows)
        {
            var trackingNos = (results ?? new List<FtzMainQueryViewModel>())
                .Where(x => x.NotGciDetails != null)
                .SelectMany(x => x.NotGciDetails)
                .Select(x => x.hwb)
                .Concat((unreceivedUploadRows ?? Enumerable.Empty<FtzMainUploadExcelRow>()).Select(x => x.BagNo))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (trackingNos.Count == 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return DataCenterDb.AirDetains
                .AsNoTracking()
                .WhereBulkContains(DataCenterDb, trackingNos, x => x.TrackingNo, x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => FormatAirDetainModel(x.Select(y => y.Model).FirstOrDefault()),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得 AIR_DETAIN 狀態顯示文字。
        /// </summary>
        private string GetAirDetainStatus(Dictionary<string, string> statusLookup, string hwb)
        {
            if (statusLookup == null || string.IsNullOrWhiteSpace(hwb))
            {
                return "";
            }

            string status;
            return statusLookup.TryGetValue(hwb.Trim(), out status) ? status : "";
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
                return "G類無ID";
            }

            return model ?? "";
        }
    }
}
