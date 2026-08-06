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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Service.Services.Ftz
{
    public partial class FtzService
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
                CalculateMainUploadStatistics(results, uploadRows);
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

            // 2. 批次查詢分提單號的派件公司
            if (allHwbs.Any())
            {
                airTransNameLookup = GetAirTransNameLookup();
                trackingNoDict = GetOriginalTransNameLookup(allHwbs, true, true, airTransNameLookup);
            }

            // 3. 批次查詢袋號的派件公司
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

            var normalizedValue = value.Trim().Replace(",", "");
            if (int.TryParse(normalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalResult))
            {
                return Convert.ToInt32(decimalResult);
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
                var d1Date = now.AddDays(-30); // now - 30天

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
        /// 讀取主號查詢上傳 Excel 的資料。
        /// </summary>
        /// <param name="uploadStream">上傳檔案串流。</param>
        /// <returns>上傳 Excel 的解析結果。</returns>
        public FtzMainUploadExcelData ReadMainUploadData(Stream uploadStream)
        {
            var uploadData = new FtzMainUploadExcelData();
            if (uploadStream == null)
            {
                return uploadData;
            }

            // 同一個上傳檔只建立一次 workbook，避免重複讀取 HttpPostedFileBase.InputStream 時發生空串流。
            if (uploadStream.CanSeek)
            {
                uploadStream.Position = 0;
                if (uploadStream.Length == 0)
                {
                    throw new Exception("上傳檔案內容為空，請重新選擇檔案後再匯出");
                }
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

            uploadData.DetailRows = ReadMainUploadDetailRows(workbook);
            uploadData.SummaryRows = ReadMainUploadSummaryRows(workbook);
            return uploadData;
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

            return ReadMainUploadDetailRows(workbook);
        }

        /// <summary>
        /// 讀取主號查詢上傳 Excel 的明細頁籤。
        /// </summary>
        private List<FtzMainUploadExcelRow> ReadMainUploadDetailRows(IWorkbook workbook)
        {
            // 需求只接受「明細」頁籤，其他頁籤資料一律不處理。
            var sheet = workbook.GetSheet("明細");
            if (sheet == null)
            {
                throw new Exception("找不到 Excel 頁籤：明細");
            }

            // 先定位表頭，之後才能依欄名讀取主號、袋號與收單註記。
            var headerInfo = FindMainUploadHeader(sheet);
            var headerMap = headerInfo.Item2;
            var requiredHeaders = new[] { "袋號", "主號", "分艙單收單註記", "1分號多件之分號", "備註" };
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
                var remark = row.GetCellData(headerMap["備註"]);

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
                    OneHwbMultiPieceHwb = (oneHwbMultiPieceHwb ?? "").Trim(),
                    Remark = IsZzzaRemark(remark) ? ZzzaRemark : ""
                });
            }

            return uploadRows;
        }

        /// <summary>
        /// 讀取主號查詢上傳 Excel 的主號2 頁籤。
        /// </summary>
        private List<FtzMainUploadSummaryRow> ReadMainUploadSummaryRows(IWorkbook workbook)
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

            var summaryRows = new List<FtzMainUploadSummaryRow>();
            for (int rowIndex = headerInfo.Item1 + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                var mwb = row.GetCellData(headerMap["主號"]);
                var totalPiece = row.GetCellData(headerMap["總件數"]);
                var transmissionTime = row.GetCellData(headerMap["傳輸時間"]);
                var importDate = row.GetCellData(headerMap["進口日期"]);
                var flightNumber = row.GetCellData(headerMap["航機班次"]);


                if (string.IsNullOrWhiteSpace(mwb))
                {
                    continue;
                }

                summaryRows.Add(new FtzMainUploadSummaryRow
                {
                    Mwb = mwb.Trim(),
                    TotalPiece = (totalPiece ?? "").Trim(),
                    TransmissionTime = (transmissionTime ?? "").Trim(),
                    ImportDate = (importDate ?? "").Trim(),
                    FlightNumber = (flightNumber ?? "").Trim()
                });
            }

            return summaryRows;
        }

        /// <summary>
        /// 尋找主號上傳 Excel 的表頭列。
        /// </summary>
        /// <param name="sheet">工作表。</param>
        /// <returns>表頭列索引與欄位對照。</returns>
        private Tuple<int, Dictionary<string, int>> FindMainUploadHeader(ISheet sheet)
        {
            var requiredHeaders = new[] { "袋號", "主號", "分艙單收單註記", "1分號多件之分號", "備註" };
            return FindUploadHeader(sheet, requiredHeaders);
        }

        /// <summary>
        /// 尋找上傳 Excel 的表頭列。
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
                        .Select(rowGroup => rowGroup
                            .OrderByDescending(IsZzzaUploadRow)
                            .First())
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判斷上傳明細的備註是否為 ZZZA。
        /// </summary>
        private bool IsZzzaRemark(string remark)
        {
            return string.Equals(
                (remark ?? "").Trim(),
                ZzzaRemark,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判斷上傳明細是否有 ZZZA 註記。
        /// </summary>
        private bool IsZzzaUploadRow(FtzMainUploadExcelRow row)
        {
            return row != null && IsZzzaRemark(row.Remark);
        }

        /// <summary>
        /// 取得指定主號已收單且仍未進倉的 ZZZA 上傳明細。
        /// </summary>
        private List<FtzMainUploadExcelRow> GetZzzaReceivedUploadRows(
            FtzMainQueryViewModel mainItem,
            Dictionary<string, List<FtzMainUploadExcelRow>> uploadRowsByMwb)
        {
            if (mainItem == null || mainItem.NotGciDetails == null || uploadRowsByMwb == null)
            {
                return new List<FtzMainUploadExcelRow>();
            }

            var mwb = (mainItem.Mwb ?? "").Trim();
            List<FtzMainUploadExcelRow> mainUploadRows;
            if (string.IsNullOrWhiteSpace(mwb) || !uploadRowsByMwb.TryGetValue(mwb, out mainUploadRows))
            {
                return new List<FtzMainUploadExcelRow>();
            }

            var notGciTrackingNos = new HashSet<string>(
                mainItem.NotGciDetails
                    .SelectMany(row => new[] { row.hwb, row.expBagNo })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return mainUploadRows
                .Where(IsZzzaUploadRow)
                .Where(row => notGciTrackingNos.Contains((row.BagNo ?? "").Trim()))
                .ToList();
        }

        /// <summary>
        /// 取得指定主號已出現在查詢資料、且不在未進倉明細的 ZZZA 上傳明細。
        /// </summary>
        private List<FtzMainUploadExcelRow> GetZzzaGciUploadRows(
            FtzMainQueryViewModel mainItem,
            Dictionary<string, List<FtzMainUploadExcelRow>> uploadRowsByMwb)
        {
            if (mainItem == null || mainItem.RawData?.Rows == null || uploadRowsByMwb == null)
            {
                return new List<FtzMainUploadExcelRow>();
            }

            var mwb = (mainItem.Mwb ?? "").Trim();
            List<FtzMainUploadExcelRow> mainUploadRows;
            if (string.IsNullOrWhiteSpace(mwb) || !uploadRowsByMwb.TryGetValue(mwb, out mainUploadRows))
            {
                return new List<FtzMainUploadExcelRow>();
            }

            var queryTrackingNos = new HashSet<string>(
                mainItem.RawData.Rows
                    .SelectMany(row => new[] { row.Hwb, row.ExpBagNo })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var notGciTrackingNos = new HashSet<string>(
                (mainItem.NotGciDetails ?? new List<Row>())
                    .SelectMany(row => new[] { row.hwb, row.expBagNo })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return mainUploadRows
                .Where(IsZzzaUploadRow)
                .Where(row => queryTrackingNos.Contains((row.BagNo ?? "").Trim()))
                .Where(row => !notGciTrackingNos.Contains((row.BagNo ?? "").Trim()))
                .ToList();
        }

        /// <summary>
        /// 取得未進倉明細對應的 ZZZA 顯示值。
        /// </summary>
        private string GetZzzaRemark(Row detail, IEnumerable<FtzMainUploadExcelRow> zzzaReceivedRows)
        {
            if (detail == null)
            {
                return "";
            }

            var hwb = (detail.hwb ?? "").Trim();
            var expBagNo = (detail.expBagNo ?? "").Trim();
            var hasZzza = (zzzaReceivedRows ?? Enumerable.Empty<FtzMainUploadExcelRow>())
                .Where(IsZzzaUploadRow)
                .Select(row => (row.BagNo ?? "").Trim())
                .Any(bagNo =>
                    (!string.IsNullOrWhiteSpace(hwb) && string.Equals(bagNo, hwb, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(expBagNo) && string.Equals(bagNo, expBagNo, StringComparison.OrdinalIgnoreCase)));

            return hasZzza ? ZzzaRemark : "";
        }

        /// <summary>
        /// 套用主號上傳明細的未收單與 ZZZA 統計。
        /// </summary>
        private void CalculateMainUploadStatistics(
            List<FtzMainQueryViewModel> results,
            List<FtzMainUploadExcelRow> uploadRows)
        {
            if (results == null)
            {
                return;
            }

            var uploadRowsByMwb = BuildMainUploadRowsByMwb(uploadRows);
            var unreceivedRowsByMainItem = GetUnreceivedUploadRowsByMainItem(results, uploadRowsByMwb);
            var allUnreceivedRows = unreceivedRowsByMainItem.SelectMany(x => x.Value).ToList();
            SetUnreceivedTransName(allUnreceivedRows);
            var airDetainStatusLookup = GetAirDetainStatusLookup(results, allUnreceivedRows);

            foreach (var item in results)
            {
                List<FtzMainUploadExcelRow> unreceivedRows;
                if (!unreceivedRowsByMainItem.TryGetValue(item, out unreceivedRows))
                {
                    unreceivedRows = new List<FtzMainUploadExcelRow>();
                }

                // 上傳檔標記 ZZZA，且能對應到 FTZ 未進倉明細的資料，視為「ZZZA收單」。
                var zzzaReceivedRows = GetZzzaReceivedUploadRows(item, uploadRowsByMwb);
                // 上傳檔標記 ZZZA、有出現在 FTZ 主號查詢資料，且不在未進倉明細，視為「ZZZA進倉」。
                var zzzaGciRows = GetZzzaGciUploadRows(item, uploadRowsByMwb);
                foreach (var detail in item.NotGciDetails ?? new List<Row>())
                {
                    // 先將 ZZZA 註記寫入明細模型，匯出時只需輸出模型值，不再重新比對。
                    detail.ZzzaRemark = GetZzzaRemark(detail, zzzaReceivedRows);
                    detail.Status = GetAirDetainStatus(airDetainStatusLookup, detail.hwb);
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

                // G類無ID只計算未收單補列資料，不包含原本已收單的未進倉明細。
                item.GTypeNoIdCount = unreceivedRows.Count(row =>
                    string.Equals(row.Status, GTypeNoIdStatus, StringComparison.OrdinalIgnoreCase));

                // 收單件數與申報不計入 ZZZA進倉、ZZZA收單；進倉不計入 ZZZA進倉。
                item.ReceivedPieceCount =
                    ParseInt(item.HwbPiece) + ParseInt(item.ExpBagCount) -
                    item.ZzzaGciCount - item.ZzzaReceivedCount;
                item.HwbPiece = (ParseInt(item.HwbPiece) - item.ZzzaGciCount - item.ZzzaReceivedCount)
                    .ToString(CultureInfo.InvariantCulture);
                item.HwbGciPiece = (ParseInt(item.HwbGciPiece) - item.ZzzaGciCount)
                    .ToString(CultureInfo.InvariantCulture);

                // 未進倉件及未進倉小計只需排除仍在未進倉明細內的 ZZZA收單。
                item.NotGciPiece -= item.ZzzaReceivedCount;
                item.NotGciTotal -= item.ZzzaReceivedCount;

                // 派件公司統計同樣排除 ZZZA收單及 ZZZA未收單，匯出直接使用此計算結果。
                item.TransNameCounts = BuildTransNameCounts(item, unreceivedRows);
                item.TransNameSummary = BuildTransNameSummary(item.TransNameCounts);
            }
        }

        /// <summary>
        /// 計算排除 ZZZA 後的派件公司件數。
        /// </summary>
        private Dictionary<string, int> BuildTransNameCounts(
            FtzMainQueryViewModel item,
            List<FtzMainUploadExcelRow> unreceivedRows)
        {
            var transNames = (item.NotGciDetails ?? new List<Row>())
                .Where(row => string.IsNullOrEmpty(row.ZzzaRemark))
                .Select(row => NormalizeTransName(row.TransName))
                .Concat((unreceivedRows ?? new List<FtzMainUploadExcelRow>())
                    .Where(row => !IsZzzaUploadRow(row))
                    .Select(row => NormalizeTransName(row.TransName)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return transNames.ToDictionary(
                transName => transName,
                transName => GetNotGciTransNameCount(item, transName) +
                    (unreceivedRows?.Count(row =>
                        !IsZzzaUploadRow(row) && IsSameTransName(row.TransName, transName)) ?? 0),
                StringComparer.OrdinalIgnoreCase);
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
        /// 依主號整理主號2 頁籤的總件數。
        /// </summary>
        private Dictionary<string, string> BuildMainUploadTotalPieceByMwb(IEnumerable<FtzMainUploadSummaryRow> uploadRows)
        {
            return (uploadRows ?? Enumerable.Empty<FtzMainUploadSummaryRow>())
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
        private Dictionary<string, FtzMainUploadSummaryRow> BuildMainUploadSummaryByMwb(
            IEnumerable<FtzMainUploadSummaryRow> uploadRows)
        {
            return (uploadRows ?? Enumerable.Empty<FtzMainUploadSummaryRow>())
                .Where(row => !string.IsNullOrWhiteSpace(row.Mwb))
                .GroupBy(row => row.Mwb.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(row => ParseUploadTransmissionTime(row.TransmissionTime))
                        .First(),
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
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/M/d H:mm:ss",
                "yyyy-MM-dd HH:mm",
                "yyyy/MM/dd HH:mm",
                "yyyy/M/d H:mm",
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyy/M/d",
                "yyyyMMddHHmmss",
                "yyyyMMdd"
            };

            DateTime result;
            var trimmedValue = value.Trim();
            if (DateTime.TryParseExact(trimmedValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            if (DateTime.TryParse(trimmedValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// 設定未收單指定錯單類別筆數。
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
        /// 設定未收單指定錯單類別筆數。
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
        public async Task<IWorkbook> ExportMainExcel(FtzMainQueryRequest request, FtzMainUploadExcelData uploadData = null)
        {
            uploadData = uploadData ?? new FtzMainUploadExcelData();
            var uploadRows = uploadData.DetailRows ?? new List<FtzMainUploadExcelRow>();

            // 主號查詢階段已完成 ZZZA 統計與扣除，匯出只負責寫入計算完成的資料。
            var queryResult = await MainQueryAsync(request, uploadRows);

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
            var uploadTotalPieceByMwb = BuildMainUploadTotalPieceByMwb(uploadData.SummaryRows);
            var uploadSummaryByMwb = BuildMainUploadSummaryByMwb(uploadData.SummaryRows);

            var unreceivedUploadRows = results
                .SelectMany(item => item.UnreceivedRows ?? new List<FtzMainUploadExcelRow>())
                .ToList();
            var plinkErrorRowsLookup = GetPlinkErrorRowsLookup(unreceivedUploadRows);
            // ========== 第一個頁籤：主號查詢結果 ==========
            ISheet sheet = workbook.CreateSheet("Ftz主號查詢結果");

            // 建立表頭
            var headers = new List<string>
              {
                  "進口日期", "主號","客戶名稱","航班", "總袋數", "收單件數", "未進倉小計",
                  "申報", "進倉","未進倉件", "併袋", "進倉袋", "未進倉袋", "未收單件數", "未收單B6F", "G類無ID"
              };

            var transNames = results
                .SelectMany(item => item.TransNameCounts?.Keys ?? Enumerable.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            headers.AddRange(transNames);
            headers.Add("ZZZA");
            headers.Add("ZZZA進倉");
            headers.Add("ZZZA收單");
            headers.Add("ZZZA未收單");
            headers.Add("派件公司件數");
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
                var mwb = string.IsNullOrWhiteSpace(item.Mwb) ? string.Empty : item.Mwb.Trim();
                uploadSummaryByMwb.TryGetValue(mwb, out var uploadSummary);
                NpoiCell.CreateCell(dataRow, column++, uploadSummary?.ImportDate ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.Mwb ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.Customer ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, uploadSummary?.FlightNumber ?? "", dataStyle);
                string uploadTotalPiece;
                var totalBagText = "";
                // 主號2 的「總件數」作為總袋數基準，扣除同主號的無派件公司統計數量。
                if (uploadTotalPieceByMwb.TryGetValue(mwb, out uploadTotalPiece))
                {
                    int totalBagCount;
                    if (int.TryParse(uploadTotalPiece, out totalBagCount))
                    {
                        var noTransNameCount = 0;
                        if (item.TransNameCounts != null)
                        {
                            item.TransNameCounts.TryGetValue(NoTransName, out noTransNameCount);
                        }
                        totalBagText = (totalBagCount - noTransNameCount).ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // 非整數內容保留上傳檔原值，避免匯出時誤改資料。
                        totalBagText = uploadTotalPiece;
                    }
                }
                NpoiCell.CreateIntCell(
                    dataRow,
                    column++,
                    totalBagText,
                    numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ReceivedPieceCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.NotGciTotal, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.HwbPiece ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.HwbGciPiece ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.NotGciPiece, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ExpBagCount ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ExpBagGciCount ?? "", numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.NotGciBag, numberStyle);

                NpoiCell.CreateIntCell(dataRow, column++, item.UnreceivedCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.UnreceivedB6FCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.GTypeNoIdCount, numberStyle);

                // 派件公司統計：未進倉明細用「申報」加總，未收單補列每筆算 1。
                foreach (var transName in transNames)
                {
                    int totalCount;
                    item.TransNameCounts.TryGetValue(transName, out totalCount);

                    NpoiCell.CreateIntCell(dataRow, column++, totalCount, numberStyle);
                }

                NpoiCell.CreateIntCell(dataRow, column++, item.ZzzaCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ZzzaGciCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ZzzaReceivedCount, numberStyle);
                NpoiCell.CreateIntCell(dataRow, column++, item.ZzzaUnreceivedCount, numberStyle);
                NpoiCell.CreateCell(dataRow, column++, item.TransNameSummary ?? "", dataStyle);
                NpoiCell.CreateCell(dataRow, column++, item.ErrorMessage ?? "", dataStyle);
            }

            // ========== 第二個頁籤：未進倉明細 ==========
            ISheet detailSheet = workbook.CreateSheet("未進倉明細");

            // 建立表頭
            string[] detailHeaders = new string[]
            {
                "項次", "提單號碼", "分號", "報單號碼", "袋號",
                "申報", "進倉", "出倉", "報關類別", "備註", "一分號多件", "錯單類別", "錯單單號", "派件公司", "狀態", "ZZZA"
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
                        NpoiCell.CreateCell(detailDataRow, 13, NormalizeTransName(detail.TransName), dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 14, detail.Status ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 15, detail.ZzzaRemark ?? "", dataStyle);
                        detailRowIndex++;
                    }
                }

                var mainUploadRows = mainItem.UnreceivedRows ?? new List<FtzMainUploadExcelRow>();
                if (!mainUploadRows.Any())
                {
                    continue;
                }

                foreach (var uploadRow in mainUploadRows)
                {
                    // 上傳檔有、查詢結果沒有時，依需求在未進倉明細補一列未收單資料。
                    var errorRows = GetPlinkErrorRows(plinkErrorRowsLookup, uploadRow.BagNo);
                    var errorRowCount = Math.Max(errorRows.Count, 1);
                    var startRowIndex = detailRowIndex;
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
                            NpoiCell.CreateCell(detailDataRow, 15, uploadRow.Remark ?? "", dataStyle);
                        }
                        else
                        {
                            CreateBlankCells(detailDataRow, 0, 10, dataStyle);
                            NpoiCell.CreateCell(detailDataRow, 15, "", dataStyle);
                        }

                        var errorRow = errorIndex < errorRows.Count ? errorRows[errorIndex] : null;
                        NpoiCell.CreateCell(detailDataRow, 11, errorRow?.Reason ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 12, errorRow?.Hawb ?? "", dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 13, NormalizeTransName(uploadRow.TransName), dataStyle);
                        NpoiCell.CreateCell(detailDataRow, 14, uploadRow.Status ?? "", dataStyle);
                        detailRowIndex++;
                    }

                    MergeColumns(detailSheet, startRowIndex, detailRowIndex - 1, 0, 10);
                    MergeColumns(detailSheet, startRowIndex, detailRowIndex - 1, 15, 15);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 計算未進倉明細中指定派件公司的申報件數。
        /// </summary>
        private int GetNotGciTransNameCount(FtzMainQueryViewModel item, string transName)
        {
            // 派件公司欄位數量以明細頁「未進倉申報」為準；同報單號碼重複時只取第一筆。
            // 單筆申報大於 1 時，先扣除進倉件數，再進行派件公司加總。
            // 未收單沒有報單號碼，就使用分號「申報」= 1 計算
            return (item?.NotGciDetails ?? new List<Row>())
                .Where(r => string.IsNullOrEmpty(r.ZzzaRemark))
                .Where(r => IsSameTransName(r.TransName, transName))
                .Select(row => new
                {
                    Row = row,
                    Key = string.IsNullOrWhiteSpace(row.declNo)
                        ? (row.hwb ?? "").Trim()
                        : row.declNo.Trim()
                })
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First().Row)
                .Sum(r =>
                {
                    var declaredPiece = ParseInt(r.piece);
                    return declaredPiece > 1
                        ? declaredPiece - ParseInt(r.gciPiece)
                        : declaredPiece;
                });
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
            return string.IsNullOrWhiteSpace(transName)
                ? NoTransName
                : transName.Trim();
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
                var knownBagNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (mainItem.NotGciDetails != null)
                {
                    foreach (var detail in mainItem.NotGciDetails)
                    {
                        var hwb = string.IsNullOrWhiteSpace(detail.hwb) ? string.Empty : detail.hwb.Trim();
                        if (!string.IsNullOrWhiteSpace(hwb))
                        {
                            knownHwbs.Add(hwb);
                        }

                        var expBagNo = string.IsNullOrWhiteSpace(detail.expBagNo)
                            ? string.Empty
                            : detail.expBagNo.Trim();
                        if (!string.IsNullOrWhiteSpace(expBagNo))
                        {
                            knownBagNos.Add(expBagNo);
                        }
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

                        // 主號查詢結果已有併袋袋號時，上傳同袋號不列為未收單。
                        var rawExpBagNo = string.IsNullOrWhiteSpace(rawRow.ExpBagNo) ? string.Empty : rawRow.ExpBagNo.Trim();
                        if (!string.IsNullOrWhiteSpace(rawExpBagNo))
                        {
                            knownBagNos.Add(rawExpBagNo);
                        }
                    }
                }

                var candidateUnreceivedRows = mainUploadRows
                    .Where(uploadRow =>
                    {
                        var bagNo = string.IsNullOrWhiteSpace(uploadRow.BagNo) ? string.Empty : uploadRow.BagNo.Trim();
                        return !string.IsNullOrWhiteSpace(bagNo) &&
                            !knownHwbs.Contains(bagNo) &&
                            !knownBagNos.Contains(bagNo);
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
