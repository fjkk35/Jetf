using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NPOI.OpenXmlFormats.Spreadsheet;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models.ShortOverUnload;
using Service.Services.CptTradeVan;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.ShortOverUnload
{
    /// <summary>
    /// 短溢卸資料查詢、Excel 匯入及 NPOI 報告產生。
    /// </summary>
    public class ShortOverUnloadService : _BaseService
    {
        private readonly CptPortalApi _portalApi;

        /// <summary>
        /// 初始化短溢卸服務及關港貿查詢 API。
        /// </summary>
        /// <param name="jetfDbContext">捷豐資料庫內容。</param>
        /// <param name="dataCenterDbContext">製單資料中心內容。</param>
        /// <param name="portalApi">關港貿單一窗口查詢 API。</param>
        public ShortOverUnloadService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext,
            CptPortalApi portalApi)
            : base(jetfDbContext, dataCenterDbContext)
        {
            // DbContext 由基底服務管理，API 在本次匯入期間共用。
            _portalApi = portalApi;
        }

        /// <summary>
        /// 驗證資料來源與匯入內容、補齊資料並產生短卸或溢卸報告。
        /// </summary>
        /// <param name="input">上傳 Excel 的資料串流。</param>
        /// <param name="format">台中-短卸或台中-溢卸。</param>
        /// <param name="importedAt">本次匯入的台北日期時間。</param>
        /// <returns>可供下載的報告及畫面核對使用的未取得資料清單。</returns>
        public async Task<ShortOverUnloadExportResult> GenerateAsync(Stream input, ShortOverUnloadDataSource format, DateTime importedAt)
        {
            // 先驗證資料來源及 Excel，避免無效檔案觸發資料庫或 API 查詢。
            if (format != ShortOverUnloadDataSource.TaichungShortUnload
                && format != ShortOverUnloadDataSource.TaichungOverUnload)
                throw new InvalidDataException("請選擇台中-短卸或台中-溢卸。");

            // 解析上傳檔的四欄資料並驗證表頭，保留原始匯入順序。
            var rows = ReadExcel(input);
            var vessels = new Dictionary<string, ShortOverUnloadVessel>();
            var vesselCache = new Dictionary<string, ShortOverUnloadVessel>();

            // 相同主提單的資料集中補值；不同主提單若使用相同船掛則共用 API 結果。
            foreach (var group in rows.GroupBy(row => row.MainNumber))
            {
                // 依主號取得 TYPE=O 的船掛，並補入各分號的裝貨型態及 ITEM_NO=1 製單資料。
                var registration = LoadOrderData(group.Key, group.ToList());
                if (!vesselCache.TryGetValue(registration, out var vessel))
                {
                    // 此船掛尚未查詢時，透過 GB330、GB325 補齊船舶及到港資料，再快取供其他主號共用。
                    vessel = await LoadVesselAsync(registration);
                    vesselCache.Add(registration, vessel);
                }
                vessels.Add(group.Key, vessel);
                if (format == ShortOverUnloadDataSource.TaichungOverUnload)
                {
                    // 彙整此主號 ER、SEO 的件數及毛重，供頁籤五 BL 顯示合計。
                    LoadBillOfLadingTotals(group.Key, group.ToList());
                    // 依有效毛重製單補齊各分號的貨物、進口人及貨櫃資料，重複資料優先取溢卸註記 O。
                    LoadOverUnloadCargo(group.Key, group.ToList());
                    // 透過 GB326 取得此主號的主艙號，以及各不同分號的分艙號。
                    await LoadOverUnloadManifestsAsync(registration, group.Key, group.ToList());
                }
            }

            // 補值完成後產生報告及核對清單，各類頁籤依自己的每頁筆數分頁。
            return new ShortOverUnloadExportResult
            {
                // 依主號建立報告；溢卸另附申請書、裝機明細、更正申請書、BL 及准單申請書。
                Content = Export(rows, vessels, importedAt, format),
                // 從已補值的資料整理缺少欄位，不重新查詢資料庫或 API。
                MissingData = FindMissingData(rows, vessels, format)
            };
        }

        /// <summary>
        /// 使用 EF 取得船掛，依主提單及匯入分提單補齊裝貨型態、貨物說明及櫃號，多筆資料取第一筆。
        /// </summary>
        /// <param name="mainNumber">主提單號碼。</param>
        /// <param name="rows">待補值的主提單明細。</param>
        /// <returns>供 GB330、GB325 查詢使用的船掛。</returns>
        private string LoadOrderData(string mainNumber, List<ShortOverUnloadRow> rows)
        {
            // 船掛取查詢結果的第一筆，查無資料時仍繼續補齊其他欄位。
            var registration = DataCenterDb.CesMainOrders
                .AsNoTracking()
                .Where(order => order.MainNumber == mainNumber && order.Type == "O")
                .Select(order => order.FieldA == null || order.FieldA.Trim() == ""
                    ? null
                    : order.FieldA.Trim())
                .FirstOrDefault();

            // 分提單先去重，再於資料庫端篩選，避免載入同主號下未匯入的資料。
            var blNos = rows
                .Select(row => row.BlNo)
                .Distinct()
                .ToList();

            // 每個主提單的艙單及製單資料各查詢一次，同一分提單取第一筆建立字典。
            var manifests = JetfDb.SeaManifestUploads
                .AsNoTracking()
                .Where(manifest => manifest.MainNumber == mainNumber && blNos.Contains(manifest.BlNo))
                .Select(manifest => new { manifest.BlNo, manifest.Manifest })
                .ToList()
                .GroupBy(row => row.BlNo?.Trim() ?? "")
                .ToDictionary(group => group.Key, group => group.First());

            var items = DataCenterDb.SeaOrderEdits
                .AsNoTracking()
                .Where(item => item.MainNumber == mainNumber
                    && item.ItemNo == "1"
                    && blNos.Contains(item.BlNo))
                .Select(item => new { item.BlNo, item.ItemName, ContainerNo = item.ContNo })
                .ToList()
                .GroupBy(row => row.BlNo?.Trim() ?? "")
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var row in rows)
            {
                // 各來源獨立補值，缺少其中一筆資料時只將對應欄位留空。
                manifests.TryGetValue(row.BlNo, out var manifest);
                items.TryGetValue(row.BlNo, out var item);
                row.Manifest = manifest?.Manifest?.Trim();
                row.ItemName = item?.ItemName;
                row.ContainerNo = item?.ContainerNo;
            }
            return registration?.Trim() ?? "";
        }

        /// <summary>
        /// 透過 GB330 與 GB325 取得報告表頭所需的船舶資料，符合船掛的資料取第一筆。
        /// </summary>
        /// <param name="registration">海關通關號碼（船掛）。</param>
        /// <returns>船舶代碼、航次、船名、受理關別及預定到港日期。</returns>
        private async Task<ShortOverUnloadVessel> LoadVesselAsync(string registration)
        {
            // 沒有船掛時不呼叫 GB 查詢，船舶欄位保持空白。
            var vessel = new ShortOverUnloadVessel();
            if (string.IsNullOrWhiteSpace(registration))
                return vessel;
            vessel.VslRegNo = registration;

            // GB330 以船掛查詢船舶代碼、航次及船名。
            var gb330 = await _portalApi.GetGb330Async(new Dictionary<string, string>
            {
                { "choice", "V" },
                { "vslRegNo", registration },
                { "shipName", "" },
                { "voyageFlightNo", "" },
                { "voyageNo", "" },
                { "estArDate", "" },
                { "shipCoCd", "" },
                { "estArDateB", "" },
                { "estArDateE", "" },
                { "custCd", "" },
                { "clearanceDateB", "" },
                { "clearanceDateE", "" },
                { "shipClass", "A" },
                { "custCd_P", "" },
                { "estArDateB_P", "" },
                { "estArDateE_P", "" },
                { "shipCoCd_P", "" }
            });

            if (gb330?.Msg?.Contains("查無資料") == true)
                gb330 = null;
            // 查無資料可留白；有回應但執行失敗時，才依 API 狀態及訊息中止匯出。
            if (gb330 != null && !IsSuccessful(gb330.Status, gb330.Msg))
                throw new InvalidDataException($"船掛「{registration}」GB330 查詢失敗：{gb330?.Msg}");

            // 明細優先取第一筆符合船掛的資料；沒有明細時相容直接回傳表頭欄位的格式。
            var match = gb330?.GridModel?
                .FirstOrDefault(row => string.Equals(row.VslRegNo?.Trim(), registration, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                vessel.ImoNo = match.ImoNo;
                vessel.VoyageFlightNo = match.VoyageFlightNo;
                vessel.ShipName = match.VslName;
                vessel.VslSign = match.VslSign;
                vessel.ShipCoCd = match.ShipCoCd;
            }
            else if (gb330 != null && (gb330.GridModel == null || gb330.GridModel.Count == 0)
                && string.Equals(gb330.VslRegNo?.Trim(), registration, StringComparison.OrdinalIgnoreCase))
            {
                vessel.ImoNo = gb330.ImoNo;
                vessel.VoyageFlightNo = gb330.VoyageFlightNo;
                vessel.ShipName = gb330.ShipName;
                vessel.VslSign = gb330.VslSign;
                vessel.ShipCoCd = gb330.ShipCoCd;
            }
            // GB325 使用相同船掛查詢受理關別、預定到港日、前一國外港及船長姓名。
            var gb325 = await _portalApi.GetGb325Async(new Dictionary<string, string>
            {
                { "rows", "10" },
                { "page", "1" },
                { "libraCopy", "" },
                { "vslRegNo", registration },
                { "take", "10" },
                { "skip", "0" },
                { "pageSize", "10" }
            });
            if (gb325?.Msg?.Contains("查無資料") == true)
                gb325 = null;
            // GB325 獨立檢查執行狀態，查無資料不影響已取得的 GB330 船舶欄位。
            if (gb325 != null && !IsSuccessful(gb325.Status, gb325.Msg))
                throw new InvalidDataException($"船掛「{registration}」GB325 查詢失敗：{gb325?.Msg}");

            // 僅使用符合船掛的資料；缺少日期不影響其他欄位，無效日期留空。
            if (gb325 != null && (string.IsNullOrWhiteSpace(gb325.VslRegNo)
                || string.Equals(gb325.VslRegNo.Trim(), registration, StringComparison.OrdinalIgnoreCase)))
            {
                vessel.ProcessCustCd = gb325.ProcessCustCd;
                vessel.LastPorCd = gb325.LastPorCd;
                vessel.OfficerName = gb325.OfficerName;
                if (DateTime.TryParseExact(gb325.EstArDate?.Trim(),
                    new[] { "yyyyMMdd", "yyyy/M/d", "yyyy-MM-dd" }, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var arrivalDate))
                    vessel.EstimatedArrivalDate = arrivalDate;
            }
            return vessel;
        }

        /// <summary>
        /// 判斷關港貿 API 回應是否表示執行成功。
        /// </summary>
        /// <param name="status">API 回傳的狀態碼。</param>
        /// <param name="message">API 回傳的訊息。</param>
        /// <returns>狀態為 ok 或訊息包含執行成功時為 true。</returns>
        private static bool IsSuccessful(string status, string message)
        {
            // 同時相容狀態碼及文字訊息兩種成功回應。
            return string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase)
                || message?.Contains("執行成功") == true;
        }

        /// <summary>每個主號一次載入有效毛重製單資料，重複分號優先選用 MERGE_OVER_FLAG=O。</summary>
        private void LoadOverUnloadCargo(string mainNumber, List<ShortOverUnloadRow> rows)
        {
            // 只載入本次匯入分號且 GW > 0 的製單，依 ROW_ID 排序後按分號分組。
            var blNos = rows.Select(row => row.BlNo).Distinct().ToList();
            var candidates = DataCenterDb.SeaOrderEdits.AsNoTracking()
                .Where(item => item.MainNumber == mainNumber && blNos.Contains(item.BlNo) && item.Gw > 0)
                .OrderBy(item => item.RowId)
                .Select(item => new ShortOverUnloadCargo
                {
                    BlNo = item.BlNo,
                    MergeOverFlag = item.MergeOverFlag,
                    JetfId = item.JetfId,
                    Gw = item.Gw,
                    Piece = item.Piece,
                    PieceUnit = item.PieceUnit,
                    Marks = item.Marks,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Importer = item.Importer,
                    ImporterAddress = item.ImporterAddress,
                    ContainerType = item.ContType,
                    ContainerNo = item.ContNo,
                    SealNo = item.SealNo
                }).ToList()
                .GroupBy(item => item.BlNo?.Trim() ?? "")
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var row in rows)
            {
                // 依上傳的分提單號碼，取得同主號下有效毛重（GW > 0）的製單資料。
                candidates.TryGetValue(row.BlNo, out var items);
                // 優先取 MERGE_OVER_FLAG=O 的第一筆；若無 O，則依 ROW_ID 順序取第一筆。
                // 查無製單資料時建立空白物件，讓對應欄位保留空白並繼續匯出。
                row.Cargo = items?.FirstOrDefault(item => item.MergeOverFlag?.Trim() == "O")
                    ?? items?.FirstOrDefault() ?? new ShortOverUnloadCargo();
            }
        }

        /// <summary>主艙號每個主號查一次；分艙號每個不同分號查一次。</summary>
        private async Task LoadOverUnloadManifestsAsync(string registration, string mainNumber,
            List<ShortOverUnloadRow> rows)
        {
            // 分號傳空字串，以「船掛＋主號」查詢 GB326 的主艙號；同一主號只查一次。
            var mainManifestNo = await LoadManifestNumberAsync(registration, mainNumber, "");
            var houseManifests = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                // 同一主號下重複出現的分號共用查詢結果，查無資料的結果也會快取。
                if (!houseManifests.TryGetValue(row.BlNo, out var manifestNo))
                {
                    // 以「船掛＋主號＋分號」查詢 GB326，將回傳的艙單號碼作為此分號的分艙號。
                    manifestNo = await LoadManifestNumberAsync(registration, mainNumber, row.BlNo);
                    houseManifests.Add(row.BlNo, manifestNo);
                }
                // 每筆明細共用主艙號，分艙號則使用該分號的查詢或快取結果。
                row.MainManifestNo = mainManifestNo;
                row.ManifestNo = manifestNo;
            }
        }

        /// <summary>沿用 GB326 query1；分號空白查主艙號，有分號時查對應分艙號。</summary>
        private async Task<string> LoadManifestNumberAsync(string registration, string mainNumber, string blNo)
        {
            // 缺少船掛時無法查詢，回傳空值讓匯出欄位留白。
            if (string.IsNullOrWhiteSpace(registration))
                return null;

            // 呼叫 GB326 的 query1，使用主號查詢模式；有傳分號時再限縮至該分提單。
            var result = await _portalApi.GetGb326Async(new Dictionary<string, string>
            {
                { "tab1.currentPage", "1" },
                { "tab1.rowNum", "10" },
                { "tab1.hideDeclNo", "" },
                { "tab1.vslRegNo", registration },
                { "tab1.mftNo", "" },
                { "choice", "B" },
                { "tab1.mawb", mainNumber },
                { "tab1.hawb", blNo }
            });
            var description = $"船掛「{registration}」主提單「{mainNumber}」{(string.IsNullOrEmpty(blNo) ? "主艙號" : $"分提單「{blNo}」分艙號")}";
            if (result == null || result.Msg?.Contains("查無資料") == true)
                return null;
            // 區分查無資料與 API 執行失敗，只有執行失敗才回報錯誤。
            if (!IsSuccessful(result.Status, result.Msg))
                throw new InvalidDataException($"{description} GB326 查詢失敗：{result?.Msg}");
            // 艙單號碼必須屬於本次查詢的船掛、主號及指定分號，避免填入其他提單的資料。
            if (string.IsNullOrWhiteSpace(result.MftNo)
                || !string.Equals(result.VslRegNo?.Trim(), registration, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(result.Mawb?.Trim(), mainNumber, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(blNo)
                    && !string.Equals(result.Hawb?.Trim(), blNo, StringComparison.OrdinalIgnoreCase)))
                return null;
            return result.MftNo.Trim();
        }

        /// <summary>每個主號一次取得 ER、SEO 的件數與毛重；同類型多筆取第一筆。</summary>
        private void LoadBillOfLadingTotals(string mainNumber, List<ShortOverUnloadRow> rows)
        {
            // 同一主號的 ER、SEO 一次載入；同類型有多筆時，依 ROW_ID 取第一筆。
            var orders = DataCenterDb.CesMainOrders.AsNoTracking()
                .Where(order => order.MainNumber == mainNumber && (order.Type == "ER" || order.Type == "SEO"))
                .OrderBy(order => order.RowId)
                .Select(order => new { order.Type, order.CountRecord, order.SumWeight })
                .ToList();
            var er = orders.FirstOrDefault(order => order.Type == "ER");
            var seo = orders.FirstOrDefault(order => order.Type == "SEO");

            // 件數與毛重各自判斷：任一來源有值就加總，缺少的一方以零計算；兩方都沒有值才留空。
            long? packages = null;
            if (er?.CountRecord != null || seo?.CountRecord != null)
            {
                long erPackages = er?.CountRecord ?? 0;
                long seoPackages = seo?.CountRecord ?? 0;
                packages = erPackages + seoPackages;
            }

            decimal? weight = null;
            if (er?.SumWeight != null || seo?.SumWeight != null)
            {
                // ER 毛重先捨去全部小數，SEO 毛重保留原精度。
                var erWeight = Math.Truncate(er?.SumWeight ?? 0d);
                var seoWeight = seo?.SumWeight ?? 0d;
                // 轉為 decimal 後加總，再四捨五入至小數點後兩位，供 BL F16 使用。
                var totalWeight = Convert.ToDecimal(erWeight) + Convert.ToDecimal(seoWeight);
                weight = Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero);
            }

            foreach (var row in rows)
            {
                row.BillOfLadingPackageCount = packages;
                row.BillOfLadingGrossWeight = weight;
            }
        }

        /// <summary>依主號、分號整理未取得欄位，合併重複明細，不追加資料庫或 API 查詢。</summary>
        private static List<ShortOverUnloadMissingData> FindMissingData(List<ShortOverUnloadRow> rows,
            IDictionary<string, ShortOverUnloadVessel> vessels, ShortOverUnloadDataSource format)
        {
            var result = new List<ShortOverUnloadMissingData>();
            // 同一主號、分號即使在上傳檔重複出現，畫面也只顯示一筆。
            foreach (var group in rows.GroupBy(row => new { row.MainNumber, row.BlNo }))
            {
                var vessel = vessels[group.Key.MainNumber];
                var missingFields = new List<string>();

                foreach (var row in group)
                {
                    // 直接列出欄位名稱與補值結果，短卸、溢卸共用相同的基本資料檢查。
                    var fields = new Dictionary<string, object>
                    {
                        { "船掛", vessel.VslRegNo },
                        { "裝貨型態", row.Manifest },
                        { "櫃號", row.ContainerNo },
                        { "船（機）代碼（IMO NO）", vessel.ImoNo },
                        { "船舶航次", vessel.VoyageFlightNo },
                        { "船名", vessel.ShipName },
                        { "受理關別", vessel.ProcessCustCd },
                        { "預定到港日期", vessel.EstimatedArrivalDate }
                    };

                    if (format == ShortOverUnloadDataSource.TaichungShortUnload)
                    {
                        // 短卸貨名使用 ITEM_NO=1 製單的貨物名稱。
                        fields.Add("貨物名稱", row.ItemName);
                    }
                    else
                    {
                        // 溢卸額外檢查附件需要的船舶、艙號、BL 合計及選定製單欄位。
                        var cargo = row.Cargo;
                        fields.Add("船舶呼號", vessel.VslSign);
                        fields.Add("船公司代碼", vessel.ShipCoCd);
                        fields.Add("到港前一國外港", vessel.LastPorCd);
                        fields.Add("船長姓名", vessel.OfficerName);
                        fields.Add("BL總件數", row.BillOfLadingPackageCount);
                        fields.Add("BL總毛重", row.BillOfLadingGrossWeight);
                        fields.Add("主艙號", row.MainManifestNo);
                        fields.Add("分艙號", row.ManifestNo);
                        fields.Add("快遞業者統一編號", cargo?.JetfId);
                        fields.Add("毛重", cargo?.Gw);
                        fields.Add("件數", cargo?.Piece);
                        fields.Add("件數單位", cargo?.PieceUnit);
                        fields.Add("標記", cargo?.Marks);
                        fields.Add("貨物名稱", cargo?.ItemName);
                        fields.Add("數量", cargo?.Quantity);
                        fields.Add("單價金額", cargo?.UnitPrice);
                        fields.Add("進口人英文名稱", cargo?.Importer);
                        fields.Add("進口人英文地址", cargo?.ImporterAddress);
                        fields.Add("貨櫃種類", cargo?.ContainerType);
                        fields.Add("貨櫃號碼", cargo?.ContainerNo);
                        fields.Add("封條號碼", cargo?.SealNo);
                    }

                    // 只將 null 或空白文字視為缺少資料，數值零仍是有效值。
                    missingFields.AddRange(fields
                        .Where(field => field.Value == null
                            || (field.Value is string text && string.IsNullOrWhiteSpace(text)))
                        .Select(field => field.Key));
                }

                if (missingFields.Count == 0)
                    continue;

                // 合併重複明細的缺少欄位並去重，直接回傳畫面需要的三個欄位。
                result.Add(new ShortOverUnloadMissingData
                {
                    MainNumber = group.Key.MainNumber,
                    BlNo = group.Key.BlNo,
                    MissingFields = string.Join("、", missingFields.Distinct())
                });
            }
            return result;
        }

        /// <summary>
        /// 讀取各非空白頁籤，檢查欄名、提單號碼並轉換件數。
        /// </summary>
        /// <param name="input">上傳 Excel 的資料串流。</param>
        /// <returns>保留匯入順序的短溢卸明細。</returns>
        private static List<ShortOverUnloadRow> ReadExcel(Stream input)
        {
            // 每個資料頁籤都使用相同四欄表頭，驗證失敗時直接在訊息指出工作表及列號。
            var headers = new[] { "主提單號碼", "分提單號碼", "申報", "實到" };
            var result = new List<ShortOverUnloadRow>();
            // 由檔案內容判斷並開啟 xls 或 xlsx，不依副檔名選擇讀取方式。
            var workbook = WorkbookFactory.Create(input);
            try
            {
                var formatter = new DataFormatter(CultureInfo.InvariantCulture);
                // 讀取公式儲存格時搭配格式化工具取得計算後的顯示文字。
                var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                for (var sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    if (sheet.PhysicalNumberOfRows == 0)
                        continue;

                    // 先確認表頭欄位及順序，再讀取資料列。
                    var header = sheet.GetRow(0);
                    for (var column = 0; column < headers.Length; column++)
                    {
                        if (header?.GetCellData(column) != headers[column])
                            throw new InvalidDataException($"工作表「{sheet.SheetName}」第 1 列第 {column + 1} 欄需為「{headers[column]}」。請下載短卸範例填寫。");
                    }

                    for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        // 四欄都先轉成顯示文字，供空白列判斷及主、分提單號碼驗證。
                        var values = Enumerable.Range(0, 4)
                            .Select(column => ReadText(row?.GetCell(column), formatter, evaluator)).ToArray();
                        // 完全空白的資料列略過，部分填寫的列仍需接受完整欄位檢查。
                        if (values.All(string.IsNullOrWhiteSpace))
                            continue;

                        var location = $"工作表「{sheet.SheetName}」第 {rowIndex + 1} 列";
                        if (string.IsNullOrWhiteSpace(values[0]) || string.IsNullOrWhiteSpace(values[1]))
                            throw new InvalidDataException($"{location}：主提單號碼及分提單號碼不可空白。");

                        // 件數沿用 GetCellData 與 ToInt，空白或無法轉換的內容使用零。
                        result.Add(new ShortOverUnloadRow
                        {
                            MainNumber = values[0],
                            BlNo = values[1],
                            DeclaredQuantity = row.GetCellData(2).ToInt(),
                            ActualQuantity = row.GetCellData(3).ToInt()
                        });
                    }
                }
            }
            finally
            {
                workbook.Close();
            }

            if (result.Count == 0)
                throw new InvalidDataException("Excel 沒有可匯入的短溢卸明細。");
            return result;
        }

        /// <summary>
        /// 產生供使用者填寫的四欄 Excel 範例。
        /// </summary>
        /// <returns>包含表頭及一筆示範資料的 Excel 位元組。</returns>
        public byte[] CreateExample()
        {
            // 範例使用與匯入驗證相同的表頭及欄位順序。
            var headers = new[] { "主提單號碼", "分提單號碼", "申報", "實到" };
            var workbook = new XSSFWorkbook();
            try
            {
                var sheet = workbook.CreateSheet("短卸匯入範例");
                var header = sheet.CreateRow(0);
                var row = sheet.CreateRow(1);
                for (var column = 0; column < headers.Length; column++)
                {
                    header.CreateCell(column).SetCellValue(headers[column]);
                    sheet.SetColumnWidth(column, (column < 2 ? 24 : 12) * 256);
                }
                // 提單號碼寫入文字，件數寫入數值，示範實到為零的短卸資料。
                row.CreateCell(0).SetCellValue("IPC261006720EX");
                row.CreateCell(1).SetCellValue("LP00846453350886");
                row.CreateCell(2).SetCellValue(1);
                row.CreateCell(3).SetCellValue(0);
                // 將範例工作簿轉為位元組，直接交由下載端回傳。
                return WriteWorkbook(workbook);
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>
        /// 安排短溢卸報告及所選格式附件，並輸出 Excel 檔案。
        /// </summary>
        /// <param name="rows">已補齊資料的匯入明細。</param>
        /// <param name="vessels">以主提單號碼索引的船舶資料。</param>
        /// <param name="importedAt">本次匯入的台北日期時間。</param>
        /// <param name="format">台中-短卸或台中-溢卸。</param>
        /// <returns>包含各主提單分頁及所選格式附件的報告 Excel 位元組。</returns>
        private static byte[] Export(List<ShortOverUnloadRow> rows,
            IDictionary<string, ShortOverUnloadVessel> vessels, DateTime importedAt, ShortOverUnloadDataSource format)
        {
            if (rows == null || rows.Count == 0)
                throw new InvalidDataException("沒有可匯出的短溢卸明細。");

            var workbook = new XSSFWorkbook();
            try
            {
                var sheetNames = new HashSet<string>();
                // 頁籤一：(一)短溢卸報告，依主號分頁，每頁 14 筆，短卸與溢卸共用。
                AddShortOverUnloadReportSheets(workbook, rows, vessels, importedAt, format, sheetNames);

                // 溢卸在報告後新增五類附件頁籤，短卸只輸出原有報告。
                if (format == ShortOverUnloadDataSource.TaichungOverUnload)
                    AddOverUnloadSheets(workbook, rows, vessels, importedAt, sheetNames);

                // 開啟報告時先顯示第一頁，再序列化為可下載的檔案。
                workbook.SetActiveSheet(0);
                return WriteWorkbook(workbook);
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>頁籤一：(一)短溢卸報告。每個主號獨立分頁，每頁 14 筆，短卸與溢卸共用。</summary>
        /// <param name="workbook">報告所屬的工作簿。</param>
        /// <param name="rows">已補齊資料的匯入明細。</param>
        /// <param name="vessels">以主提單號碼索引的船舶資料。</param>
        /// <param name="importedAt">本次匯入的台北日期時間。</param>
        /// <param name="format">台中-短卸或台中-溢卸。</param>
        /// <param name="usedNames">整份工作簿已使用的頁籤名稱。</param>
        private static void AddShortOverUnloadReportSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> rows,
            IDictionary<string, ShortOverUnloadVessel> vessels, DateTime importedAt,
            ShortOverUnloadDataSource format, ISet<string> usedNames)
        {
            // 第 8～21 列共可容納 14 筆，超過時建立下一個頁籤。
            const int rowsPerSheet = 14;
            // 工作簿共用字型及樣式，日期樣式依原始格式快取。
            var styles = CreateStyles(workbook);
            var dateStyles = new Dictionary<short, ICellStyle>();

            // 每個主提單獨立分頁，頁內資料沿用原匯入順序。
            foreach (var group in rows.GroupBy(row => row.MainNumber))
            {
                var details = group.ToList();
                var vessel = vessels[group.Key];
                var pageCount = (details.Count + rowsPerSheet - 1) / rowsPerSheet;

                // 依頁碼分頁，明細起點由每頁筆數計算。
                for (var page = 1; page <= pageCount; page++)
                {
                    var offset = (page - 1) * rowsPerSheet;
                    // 先以主號及頁碼建立合法且唯一的名稱，再產生已套用格式的空白報告。
                    var sheetName = CreateSheetName("(一)短溢卸報告", group.Key, page, usedNames);
                    var sheet = CreateSheet(workbook, sheetName, styles);

                    // 依指定儲存格填入業者代碼、船舶資料、卸存地點及日期。
                    Cell(sheet, 2, 3).SetCellValue("24951752");
                    Cell(sheet, 2, 4).SetCellValue(vessel.ImoNo);
                    Cell(sheet, 2, 6).SetCellValue(vessel.VoyageFlightNo);
                    Cell(sheet, 2, 7).SetCellValue(vessel.ProcessCustCd);
                    // C5 寫入預定到港日期，沒有日期時留白，並保留原本框線與字型。
                    SetDate(workbook, Cell(sheet, 4, 2), vessel.EstimatedArrivalDate, dateStyles);
                    Cell(sheet, 4, 3).SetCellValue(vessel.ShipName);
                    Cell(sheet, 4, 4).SetCellValue(vessel.VslRegNo);
                    // G5 為本次匯入日期，使用整份報告共用的台北時間。
                    SetDate(workbook, Cell(sheet, 4, 6), importedAt, dateStyles);
                    Cell(sheet, 4, 7).SetCellValue("TXG018DH");
                    // B23 簽章區日期與表頭使用相同匯入日期。
                    SetDate(workbook, Cell(sheet, 22, 1), importedAt, dateStyles);

                    // 明細只寫入 A8～H21，尾頁空白列保留原有框線與尺寸。
                    for (var line = 0; line < rowsPerSheet; line++)
                    {
                        for (var column = 0; column < 8; column++)
                            Cell(sheet, 7 + line, column).SetCellType(CellType.Blank);
                        if (offset + line >= details.Count)
                            continue;

                        var detail = details[offset + line];
                        Cell(sheet, 7 + line, 0).SetCellValue(detail.MainNumber);
                        Cell(sheet, 7 + line, 1).SetCellValue(detail.BlNo);
                        Cell(sheet, 7 + line, 2).SetCellValue(detail.DeclaredQuantity);
                        Cell(sheet, 7 + line, 3).SetCellValue(detail.ActualQuantity);
                        if (!string.IsNullOrEmpty(detail.Manifest))
                            Cell(sheet, 7 + line, 4).SetCellValue(detail.Manifest);
                        var itemName = format == ShortOverUnloadDataSource.TaichungOverUnload
                            ? detail.Cargo?.ItemName : detail.ItemName;
                        if (!string.IsNullOrEmpty(itemName))
                            Cell(sheet, 7 + line, 5).SetCellValue(itemName);
                        Cell(sheet, 7 + line, 6).SetCellValue(
                            format == ShortOverUnloadDataSource.TaichungOverUnload ? "溢卸" : "短卸");
                        if (!string.IsNullOrEmpty(detail.ContainerNo))
                            Cell(sheet, 7 + line, 7).SetCellValue(detail.ContainerNo);
                    }
                }
            }
        }

        /// <summary>以 NPOI 建立溢卸申請、裝機明細、更正、BL 及准單五類工作表。</summary>
        private static void AddOverUnloadSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> rows,
            IDictionary<string, ShortOverUnloadVessel> vessels, DateTime importedAt, ISet<string> usedNames)
        {
            // 五類附件共用參考版面的字型與樣式，減少分頁時重複建立格式。
            var styles = TaichungOverUnloadLayout.CreateStyles(workbook);
            var dateStyles = new Dictionary<short, ICellStyle>();
            var groups = rows.GroupBy(row => row.MainNumber).ToList();
            foreach (var group in groups)
            {
                var details = group.ToList();
                var vessel = vessels[group.Key];
                // 頁籤二：(二)溢卸申請書，每頁 14 筆，申請筆數使用此主號全部明細筆數。
                AddOverUnloadApplicationSheets(workbook, details, vessel, importedAt, styles, dateStyles, usedNames);
                // 頁籤三：(三)裝機明細表，每頁 46 筆，列出選定製單的貨物及進口人資料。
                AddCargoSheets(workbook, details, vessel, styles, usedNames);
                // 頁籤四：(四)艙單更正申請書，每頁 25 筆，分別列出主艙號及各分號的分艙號。
                AddCorrectionSheets(workbook, details, vessel, styles, usedNames);
                // 頁籤五：(五)BL，每個主號一張，彙整標記、件重及貨櫃／封條資料。
                AddBillOfLadingSheets(workbook, details, vessel, styles, usedNames);
                // 頁籤六：(六)准單申請書，每頁 14 筆，列出艙單更正後申請准單的明細。
                AddPermitApplicationSheets(workbook, details, vessel, importedAt, styles, dateStyles, usedNames);
            }
        }

        /// <summary>保留識別碼為文字，重量、件數、數量及金額寫入數值。</summary>
        private static void SetValues(ISheet sheet, int row, int column, params object[] values)
        {
            foreach (var value in values)
            {
                // 依欄位順序取得既有儲存格，保留格式並按文字、數值或空白寫入。
                var cell = Cell(sheet, row, column++);
                if (value == null) cell.SetCellType(CellType.Blank);
                else if (value is string text) cell.SetCellValue(text);
                else cell.SetCellValue(Convert.ToDouble(value));
            }
        }

        /// <summary>頁籤二：(二)溢卸申請書。每頁 14 筆，共計艙單數保留整個主號的上傳筆數。</summary>
        private static void AddOverUnloadApplicationSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> details,
            ShortOverUnloadVessel vessel, DateTime importedAt, ICellStyle[] styles,
            IDictionary<short, ICellStyle> dateStyles, ISet<string> usedNames)
        {
            for (var offset = 0; offset < details.Count; offset += 14)
            {
                // 使用溢卸申請書版面，頁籤名稱包含主號及本頁頁碼，避免不同主號撞名。
                var sheet = TaichungOverUnloadLayout.CreateSheet(workbook,
                    CreateSheetName("(二)溢卸申請書", details[0].MainNumber, offset / 14 + 1, usedNames), 0, styles);
                var pageRows = details.Skip(offset).Take(14).ToList();
                // B3 以 Excel 日期值填入預定到港日期，查無日期時留白。
                SetDate(workbook, Cell(sheet, 2, 1), vessel.EstimatedArrivalDate, dateStyles);
                Cell(sheet, 2, 3).SetCellValue(vessel.ShipName);
                Cell(sheet, 3, 1).SetCellValue(vessel.VoyageFlightNo);
                Cell(sheet, 3, 3).SetCellValue(string.Join("\n", pageRows.Select(row => row.ContainerNo).Distinct()));
                Cell(sheet, 4, 1).SetCellValue(details[0].MainNumber);
                Cell(sheet, 5, 0).SetCellValue($"由於國外漏提供資料，造成少申報分提單資料，故申請{details.Count}筆新增艙單，如蒙恩准，實感德則，謝謝!");
                Cell(sheet, 6, 1).SetCellValue(details.Count);
                for (var line = 0; line < pageRows.Count; line++)
                {
                    var row = pageRows[line];
                    // A9 起依序填入分號、分艙號、申報及實到件數，最多寫入本頁 14 筆。
                    SetValues(sheet, 8 + line, 0, row.BlNo, row.ManifestNo, row.DeclaredQuantity, row.ActualQuantity);
                }
                // A33 填入本次匯入日期，顯示格式統一為 yyyy/M/d。
                SetDate(workbook, Cell(sheet, 32, 0), importedAt, dateStyles);
            }
        }

        /// <summary>頁籤三：(三)裝機明細表。每頁 46 筆，表頭使用相同主號的船舶資料。</summary>
        private static void AddCargoSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> details,
            ShortOverUnloadVessel vessel, ICellStyle[] styles, ISet<string> usedNames)
        {
            for (var offset = 0; offset < details.Count; offset += 46)
            {
                // 使用裝機明細表版面，每 46 筆建立帶有主號及頁碼的新頁籤。
                var sheet = TaichungOverUnloadLayout.CreateSheet(workbook,
                    CreateSheetName("(三)裝機明細表", details[0].MainNumber, offset / 46 + 1, usedNames), 1, styles);
                // A3 起填入主號、船舶及前港資料；H3 依指定版面保留空白。
                SetValues(sheet, 2, 0, details[0].MainNumber, vessel.VslRegNo, vessel.VoyageFlightNo,
                    vessel.VslSign, vessel.ShipCoCd, "TXG018DH", vessel.LastPorCd, null, vessel.ImoNo);
                var pageRows = details.Skip(offset).Take(46).ToList();
                for (var line = 0; line < pageRows.Count; line++)
                {
                    var row = pageRows[line];
                    var cargo = row.Cargo ?? new ShortOverUnloadCargo();
                    // A5 起填入分號及貨物明細，識別碼保留文字，件重與金額寫入數值。
                    SetValues(sheet, 4 + line, 0, row.BlNo, cargo.JetfId, cargo.Gw, cargo.Piece, cargo.PieceUnit,
                        cargo.ItemName, cargo.Quantity, cargo.UnitPrice, cargo.Importer, cargo.ImporterAddress,
                        cargo.ContainerType, cargo.ContainerNo, cargo.SealNo);
                }
            }
        }

        /// <summary>頁籤四：(四)艙單更正申請書。每頁 25 筆，主艙號與分艙號分別填入。</summary>
        private static void AddCorrectionSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> details,
            ShortOverUnloadVessel vessel, ICellStyle[] styles, ISet<string> usedNames)
        {
            for (var offset = 0; offset < details.Count; offset += 25)
            {
                // 使用艙單更正申請書版面，每 25 筆建立帶有主號及頁碼的新頁籤。
                var sheet = TaichungOverUnloadLayout.CreateSheet(workbook,
                    CreateSheetName("(四)艙單更正申請書", details[0].MainNumber, offset / 25 + 1, usedNames), 2, styles);
                // 第 2 列填入此主號共用的船名、航次、呼號、船長及船掛。
                Cell(sheet, 1, 4).SetCellValue(vessel.ShipName);
                Cell(sheet, 1, 6).SetCellValue(vessel.VoyageFlightNo);
                Cell(sheet, 1, 7).SetCellValue(vessel.VslSign);
                Cell(sheet, 1, 8).SetCellValue(vessel.OfficerName);
                Cell(sheet, 1, 10).SetCellValue(vessel.VslRegNo);
                // B11、C11 填入主艙號與主提單號碼，與下方分艙明細分開顯示。
                SetValues(sheet, 10, 1, details[0].MainManifestNo, details[0].MainNumber);
                var pageRows = details.Skip(offset).Take(25).ToList();
                for (var line = 0; line < pageRows.Count; line++)
                {
                    var row = pageRows[line];
                    var cargo = row.Cargo ?? new ShortOverUnloadCargo();
                    // B13 起填入分艙號、分號、件重、標記、貨名及進口人資料。
                    SetValues(sheet, 12 + line, 1, row.ManifestNo, row.BlNo, cargo.Gw, cargo.Piece, cargo.PieceUnit,
                        cargo.Marks, cargo.ItemName, cargo.Importer, cargo.ImporterAddress, "TXG018DH");
                }
            }
        }

        /// <summary>頁籤五：(五)BL。依主號產生，填入合計件數、毛重、標記及櫃號／封條組合。</summary>
        private static void AddBillOfLadingSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> details,
            ShortOverUnloadVessel vessel, ICellStyle[] styles, ISet<string> usedNames)
        {
            // 每個主號建立一張 BL，套用包含底線、方框及原印章圖片的版面。
            var sheet = TaichungOverUnloadLayout.CreateSheet(workbook,
                CreateSheetName("(五)BL", details[0].MainNumber, 1, usedNames), 3, styles);
            Cell(sheet, 2, 4).SetCellValue(details[0].MainNumber);
            var voyage = string.IsNullOrWhiteSpace(vessel.VoyageFlightNo) ? null : $"V. {vessel.VoyageFlightNo}";
            // A11 組成「船名  V. 航次」，缺少的部分略過，避免留下多餘分隔空白。
            Cell(sheet, 10, 0).SetCellValue(string.Join("  ", new[] { vessel.ShipName, voyage }
                .Where(value => !string.IsNullOrWhiteSpace(value))));
            // A16 彙整同主號的非空白標記，重複標記只保留一次並換行顯示。
            Cell(sheet, 15, 0).SetCellValue(string.Join("\n", details.Select(row => row.Cargo?.Marks)
                .Where(marks => !string.IsNullOrWhiteSpace(marks)).Distinct()));
            var totals = details[0];
            // C16、F16 使用此主號已算好的合計並加上單位，毛重固定兩位小數，未取得時留白。
            Cell(sheet, 15, 2).SetCellValue(totals.BillOfLadingPackageCount.HasValue
                ? totals.BillOfLadingPackageCount.Value.ToString(CultureInfo.InvariantCulture) + "PACKAGES" : null);
            Cell(sheet, 15, 5).SetCellValue(totals.BillOfLadingGrossWeight.HasValue
                ? totals.BillOfLadingGrossWeight.Value.ToString("0.00", CultureInfo.InvariantCulture) + "KGS" : null);
            // A22 將貨櫃號碼／封條號碼／固定箱型 40HC 去重後換行，整組查無資料時不輸出。
            Cell(sheet, 21, 0).SetCellValue(string.Join("\n", details
                .Select(row => row.Cargo)
                .Where(cargo => !string.IsNullOrWhiteSpace(cargo?.ContainerNo) || !string.IsNullOrWhiteSpace(cargo?.SealNo))
                .Select(cargo => $"{cargo.ContainerNo}/{cargo.SealNo}/40HC").Distinct()));
        }

        /// <summary>頁籤六：(六)准單申請書。每頁 14 筆，共計艙單數保留整個主號的上傳筆數。</summary>
        private static void AddPermitApplicationSheets(XSSFWorkbook workbook, List<ShortOverUnloadRow> details,
            ShortOverUnloadVessel vessel, DateTime importedAt, ICellStyle[] styles,
            IDictionary<short, ICellStyle> dateStyles, ISet<string> usedNames)
        {
            for (var offset = 0; offset < details.Count; offset += 14)
            {
                // 使用准單申請書版面，每 14 筆建立帶有主號及頁碼的新頁籤。
                var sheet = TaichungOverUnloadLayout.CreateSheet(workbook,
                    CreateSheetName("(六)准單申請書", details[0].MainNumber, offset / 14 + 1, usedNames), 4, styles);
                var pageRows = details.Skip(offset).Take(14).ToList();
                // B3 填入預定到港日期，缺少時留白並保留原日期儲存格樣式。
                SetDate(workbook, Cell(sheet, 2, 1), vessel.EstimatedArrivalDate, dateStyles);
                Cell(sheet, 2, 3).SetCellValue(vessel.ShipName);
                Cell(sheet, 3, 1).SetCellValue(vessel.VoyageFlightNo);
                // D4 使用本頁選定製單的非空白貨櫃號碼，去重後換行顯示。
                Cell(sheet, 3, 3).SetCellValue(string.Join("\n", pageRows.Select(row => row.Cargo?.ContainerNo)
                    .Where(number => !string.IsNullOrWhiteSpace(number)).Distinct()));
                Cell(sheet, 4, 1).SetCellValue(details[0].MainNumber);
                Cell(sheet, 6, 1).SetCellValue(details.Count);
                for (var line = 0; line < pageRows.Count; line++)
                {
                    var row = pageRows[line];
                    // A9 起填入本頁的分號、分艙號、申報及實到件數。
                    SetValues(sheet, 8 + line, 0, row.BlNo, row.ManifestNo, row.DeclaredQuantity, row.ActualQuantity);
                }
                // A33 與其他申請頁籤使用相同匯入日期及 yyyy/M/d 格式。
                SetDate(workbook, Cell(sheet, 32, 0), importedAt, dateStyles);
            }
        }

        /// <summary>
        /// 取得儲存格顯示文字並去除前後空白。
        /// </summary>
        /// <param name="cell">待讀取的儲存格，可為 null。</param>
        /// <param name="formatter">Excel 顯示格式轉換器。</param>
        /// <param name="evaluator">Excel 公式計算器。</param>
        /// <returns>儲存格文字；不存在時回傳空字串。</returns>
        private static string ReadText(ICell cell, DataFormatter formatter, IFormulaEvaluator evaluator)
        {
            // 有儲存格時才格式化，公式儲存格使用計算後的顯示值。
            return cell == null ? "" : formatter.FormatCellValue(cell, evaluator).Trim();
        }

        /// <summary>
        /// 取得指定儲存格，列或儲存格不存在時才建立。
        /// </summary>
        /// <param name="sheet">目標工作表。</param>
        /// <param name="row">從零起算的列索引。</param>
        /// <param name="column">從零起算的欄索引。</param>
        /// <returns>可供填值的儲存格。</returns>
        private static ICell Cell(ISheet sheet, int row, int column)
        {
            // 優先重用既有儲存格，保留建立版面時套用的樣式。
            var target = sheet.GetRow(row) ?? sheet.CreateRow(row);
            return target.GetCell(column) ?? target.CreateCell(column);
        }

        /// <summary>
        /// 保留儲存格既有樣式並以 yyyy/M/d 格式寫入 Excel 日期。
        /// </summary>
        /// <param name="workbook">日期儲存格所屬的工作簿。</param>
        /// <param name="cell">目標日期儲存格。</param>
        /// <param name="date">待寫入的日期時間，沒有值時儲存格留空。</param>
        /// <param name="styles">以原始樣式索引為鍵的日期樣式快取。</param>
        private static void SetDate(IWorkbook workbook, ICell cell, DateTime? date,
            IDictionary<short, ICellStyle> styles)
        {
            if (!date.HasValue)
            {
                cell.SetCellType(CellType.Blank);
                return;
            }
            // 原始樣式相同的儲存格共用日期樣式，避免頁數增加時重複建立格式。
            var originalStyle = cell.CellStyle;
            if (!styles.TryGetValue(originalStyle.Index, out var style))
            {
                style = workbook.CreateCellStyle();
                // 複製字型、對齊與框線，只替換日期的顯示格式。
                style.CloneStyleFrom(originalStyle);
                style.DataFormat = workbook.CreateDataFormat().GetFormat("yyyy/M/d");
                styles.Add(originalStyle.Index, style);
            }
            cell.CellStyle = style;
            // 寫入真正的日期值並去除時間部分。
            cell.SetCellValue(date.Value.Date);
        }

        /// <summary>
        /// 依頁籤名稱、主提單及頁碼產生合法且不重複的 Excel 頁籤名稱。
        /// </summary>
        /// <param name="title">頁籤編號及名稱。</param>
        /// <param name="mainNumber">主提單號碼。</param>
        /// <param name="page">從一開始的分頁序號。</param>
        /// <param name="usedNames">本次匯出已使用的頁籤名稱集合。</param>
        /// <returns>不超過 31 個字元且已加入名稱集合的頁籤名稱。</returns>
        private static string CreateSheetName(string title, string mainNumber, int page, ISet<string> usedNames)
        {
            // 替換 Excel 禁用字元，並為頁碼及撞名序號預留長度。
            var safeName = WorkbookUtil.CreateSafeSheetName(title + "_" + mainNumber);
            var suffix = "_" + page;
            var collision = 1;
            while (true)
            {
                var candidate = safeName.Substring(0, Math.Min(safeName.Length, 31 - suffix.Length)) + suffix;
                if (usedNames.Add(candidate))
                    return candidate;
                // 不同長主號截斷後可能相同，增加序號直到名稱可用。
                suffix = "_" + page + "_" + ++collision;
            }
        }

        /// <summary>
        /// 將工作簿寫入記憶體串流並轉為檔案位元組。
        /// </summary>
        /// <param name="workbook">已填值完成的工作簿。</param>
        /// <returns>可供下載的 Excel 位元組。</returns>
        private static byte[] WriteWorkbook(IWorkbook workbook)
        {
            // 全程在記憶體產檔，不建立實體暫存 Excel。
            using (var output = new MemoryStream())
            {
                workbook.Write(output);
                return output.ToArray();
            }
        }

        /// <summary>
        /// 建立共用字型及儲存格樣式，索引對應參考檔的格式索引。
        /// </summary>
        /// <param name="workbook">樣式所屬的工作簿。</param>
        /// <returns>供報告版面依索引套用的儲存格樣式陣列。</returns>
        private static ICellStyle[] CreateStyles(IWorkbook workbook)
        {
            // 以旗標組合四邊框線，維持合併儲存格各邊的參考檔格式。
            const int left = 1;
            const int right = 2;
            const int top = 4;
            const int bottom = 8;
            const int allBorders = left | right | top | bottom;
            // 同一工作簿重用標題、內文及簽章字型。
            var ming = CreateFont(workbook, "新細明體", 12, 2);
            var normal = CreateFont(workbook, "標楷體", 12);
            var company = CreateFont(workbook, "標楷體", 26, bold: true);
            var companyContinuation = CreateFont(workbook, "標楷體", 26);
            var body = CreateFont(workbook, "標楷體", 14);
            var reportTitle = CreateFont(workbook, "標楷體", 20);
            var styles = new ICellStyle[36];

            // 樣式索引固定對應參考檔，包含標題、表頭、明細、日期及簽章區。
            styles[0] = Style(workbook, ming);
            styles[1] = Style(workbook, normal);
            styles[2] = Style(workbook, company, bottom);
            styles[3] = Style(workbook, companyContinuation, bottom);
            styles[4] = Style(workbook, companyContinuation);
            styles[5] = Style(workbook, body, allBorders, HorizontalAlignment.Left, wrap: true);
            styles[6] = Style(workbook, body, allBorders, HorizontalAlignment.Center, wrap: true);
            styles[7] = Style(workbook, body, allBorders);
            styles[8] = Style(workbook, reportTitle);
            styles[9] = Style(workbook, body);
            styles[10] = Style(workbook, body, left | bottom, HorizontalAlignment.Left, wrap: true);
            styles[11] = Style(workbook, body, left | right | bottom);
            styles[12] = Style(workbook, body, left | right | top, HorizontalAlignment.Left, date: true);
            ((XSSFCellStyle)styles[12]).GetCoreXf().quotePrefix = true;
            styles[13] = Style(workbook, body, left | right | top);
            styles[14] = Style(workbook, body, left | top | bottom, HorizontalAlignment.Center);
            styles[15] = Style(workbook, body, left | right | top, HorizontalAlignment.Left, date: true);
            styles[16] = Style(workbook, body, allBorders, HorizontalAlignment.Center);
            styles[17] = Style(workbook, body, right | top | bottom, HorizontalAlignment.Left, wrap: true);
            styles[18] = Style(workbook, body, alignment: HorizontalAlignment.Center);
            styles[19] = Style(workbook, body, alignment: HorizontalAlignment.Left, wrap: true);
            styles[20] = Style(workbook, body, alignment: HorizontalAlignment.Center, wrap: true);
            styles[21] = Style(workbook, normal, alignment: HorizontalAlignment.Justify,
                vertical: VerticalAlignment.Distributed);
            styles[22] = Style(workbook, normal, alignment: HorizontalAlignment.Left, date: true);
            styles[23] = Style(workbook, normal, alignment: HorizontalAlignment.Center,
                vertical: VerticalAlignment.Distributed, date: true);
            styles[24] = Style(workbook, body, allBorders, HorizontalAlignment.Center, wrap: true);
            styles[25] = Style(workbook, body, left | top, HorizontalAlignment.Left, VerticalAlignment.Top, true);
            styles[26] = Style(workbook, body, left | bottom, HorizontalAlignment.Left, VerticalAlignment.Top, true);
            styles[27] = Style(workbook, body, left | bottom, HorizontalAlignment.Center, wrap: true);
            styles[28] = Style(workbook, body, right | bottom, HorizontalAlignment.Center, wrap: true);
            styles[29] = Style(workbook, body, left | top | bottom, HorizontalAlignment.Center, wrap: true);
            styles[30] = Style(workbook, body, right | top | bottom, HorizontalAlignment.Center, wrap: true);
            styles[31] = Style(workbook, body, left | top | bottom, HorizontalAlignment.Center);
            styles[32] = Style(workbook, body, right | top | bottom, HorizontalAlignment.Center);
            styles[33] = Style(workbook, body, allBorders, HorizontalAlignment.Center);
            styles[34] = Style(workbook, body, left | top, HorizontalAlignment.Center, wrap: true);
            styles[35] = Style(workbook, body, right | top, HorizontalAlignment.Center, wrap: true);
            return styles;
        }

        /// <summary>
        /// 建立一頁 14 筆的空白報告，包含尺寸、標題、框線、合併及列印設定。
        /// </summary>
        /// <param name="workbook">報告所屬的工作簿。</param>
        /// <param name="name">已驗證合法且不重複的頁籤名稱。</param>
        /// <param name="styles">依參考檔建立的共用儲存格樣式。</param>
        /// <returns>可供填入表頭及明細的工作表。</returns>
        private static ISheet CreateSheet(IWorkbook workbook, string name, ICellStyle[] styles)
        {
            // 先設定工作表預設尺寸與縮放，再建立固定的報告區域。
            var sheet = (XSSFSheet)workbook.CreateSheet(name);
            sheet.DefaultColumnWidth = 9;
            sheet.DefaultRowHeightInPoints = 16.2f;
            sheet.GetCTWorksheet().sheetFormatPr.defaultRowHeight = 16.2;
            sheet.SetZoom(80);

            // Excel 欄寬以 1/256 字元為單位，C、G、H 欄加寬以避免表頭文字拆行。
            var widths = new[] { 6741, 6627, 5440, 5575, 2730, 9073, 5888, 6656 };
            for (var column = 0; column < widths.Length; column++)
            {
                sheet.SetColumnWidth(column, widths[column]);
                sheet.SetDefaultColumnStyle(column, styles[1]);
            }

            // 列高依參考檔逐區設定，第 8～21 列保留給明細。
            for (var rowIndex = 0; rowIndex < 24; rowIndex++)
            {
                var row = sheet.CreateRow(rowIndex);
                if (rowIndex == 0) row.HeightInPoints = 45;
                else if (rowIndex <= 4) row.HeightInPoints = 28.5f;
                else if (rowIndex == 5) row.HeightInPoints = 33;
                else if (rowIndex == 6) row.HeightInPoints = 29.25f;
                else if (rowIndex <= 20) row.HeightInPoints = 21;
                else if (rowIndex == 21) row.HeightInPoints = 22.5f;

                for (var column = 0; column < 8; column++)
                    row.CreateCell(column).CellStyle = styles[1];
            }

            // 逐列套用格式索引，保留合併區域邊緣及明細區的框線。
            ApplyStyles(sheet, 0, styles, new[] { 1, 1, 2, 3, 4, 1, 1, 1 });
            ApplyStyles(sheet, 1, styles, new[] { 1, 1, 25, 5, 24, 24, 7, 7 });
            ApplyStyles(sheet, 2, styles, new[] { 8, 9, 26, 10, 27, 28, 10, 11 });
            ApplyStyles(sheet, 3, styles, new[] { 1, 1, 7, 11, 29, 30, 11, 11 });
            ApplyStyles(sheet, 4, styles, new[] { 1, 1, 12, 13, 31, 32, 15, 13 });
            ApplyStyles(sheet, 5, styles, new[] { 33, 33, 33, 33, 34, 35, 24, 24 });
            ApplyStyles(sheet, 6, styles, new[] { 33, 33, 16, 16, 27, 28, 24, 24 });
            for (var row = 7; row <= 20; row++)
                ApplyStyles(sheet, row, styles, new[] { 16, 16, 16, 16, 14, 17, 6, 6 });
            ApplyStyles(sheet, 21, styles, new[] { 18, 18, 18, 18, 18, 19, 20, 20 });
            ApplyStyles(sheet, 22, styles, new[] { 21, 22, 1, 1, 1, 1, 1, 1 });
            ApplyStyles(sheet, 23, styles, new[] { 1, 21, 23, 21, 1, 23, 1, 1 });

            // 合併範圍固定沿用參考檔的 11 個區域。
            foreach (var range in new[]
            {
                "A6:A7", "B6:B7", "C6:D6", "E6:F7", "G6:G7", "H6:H7",
                "C2:C3", "E2:F2", "E3:F3", "E4:F4", "E5:F5"
            })
                sheet.AddMergedRegion(CellRangeAddress.ValueOf(range));

            // 寫入固定標題與欄名，動態表頭及明細由 Export 填入。
            Text(sheet, 0, 2, "華揚國際物流股份有限公司");
            Text(sheet, 1, 2, "海空運別\n□空運■海運");
            Text(sheet, 1, 3, "承攬業者代碼");
            Text(sheet, 1, 4, "船機代碼");
            Text(sheet, 1, 6, "船舶航次");
            Text(sheet, 1, 7, "受理艙單關別代碼");
            Text(sheet, 2, 0, "進口貨物短溢卸報告");
            Text(sheet, 3, 2, "進口日期");
            Text(sheet, 3, 3, "船舶名稱");
            Text(sheet, 3, 4, "海關通關號碼");
            Text(sheet, 3, 6, "進倉日期與時間");
            Text(sheet, 3, 7, "卸存地點代碼");
            Text(sheet, 5, 0, "主提單號碼");
            Text(sheet, 5, 1, "分提單號碼");
            Text(sheet, 5, 2, "貨物件數及單位(包裝說明)");
            Text(sheet, 5, 4, "貨櫃(物)裝貨型態\n貨櫃號碼");
            Text(sheet, 5, 6, "短溢卸註記");
            Text(sheet, 5, 7, "備註");
            Text(sheet, 6, 2, "申報(CTN)");
            Text(sheet, 6, 3, "實到(CTN)");
            Text(sheet, 22, 0, "日期：");
            Text(sheet, 22, 2, "倉儲業簽章：");
            Text(sheet, 22, 4, "運輸業/承攬業簽章：");
            Text(sheet, 22, 6, "關員簽章：");

            // 紙張、方向、縮放與邊界設定均沿用參考檔，確保列印版面一致。
            sheet.HorizontallyCenter = true;
            sheet.FitToPage = true;
            sheet.GetCTWorksheet().sheetPr.tabColor = new CT_Color
            {
                theme = 4,
                themeSpecified = true,
                tint = 0.59999389629810485,
                tintSpecified = true
            };
            sheet.SetMargin(MarginType.LeftMargin, 0.11811023622047245);
            sheet.SetMargin(MarginType.RightMargin, 0.11811023622047245);
            sheet.SetMargin(MarginType.TopMargin, 0);
            sheet.SetMargin(MarginType.BottomMargin, 0);
            sheet.PrintSetup.Landscape = true;
            sheet.PrintSetup.PaperSize = 9; // 參考檔的 OOXML 紙張代碼：A4。
            sheet.PrintSetup.Scale = 81;
            sheet.PrintSetup.FitWidth = 1;
            sheet.PrintSetup.FitHeight = 2;
            sheet.PrintSetup.HeaderMargin = 0.31496062992125984;
            sheet.PrintSetup.FooterMargin = 0.31496062992125984;
            return sheet;
        }

        /// <summary>
        /// 建立符合參考檔繁體中文字集設定的字型。
        /// </summary>
        /// <param name="workbook">字型所屬的工作簿。</param>
        /// <param name="name">字型名稱。</param>
        /// <param name="size">字級，單位為點。</param>
        /// <param name="family">Excel 字型家族代碼。</param>
        /// <param name="bold">是否使用粗體。</param>
        /// <returns>可供儲存格樣式共用的字型。</returns>
        private static IFont CreateFont(IWorkbook workbook, string name, int size, int family = 4, bool bold = false)
        {
            // 明確設定字型名稱、字級及繁體中文字集，避免匯出後套用預設字型。
            var font = (XSSFFont)workbook.CreateFont();
            font.FontName = name;
            font.FontHeightInPoints = size;
            font.GetCTFont().AddNewCharset().val = 136;
            font.Family = family;
            font.IsBold = bold;
            return font;
        }

        /// <summary>
        /// 依字型、框線旗標、對齊與換行設定建立儲存格樣式。
        /// </summary>
        /// <param name="workbook">樣式所屬的工作簿。</param>
        /// <param name="font">共用字型。</param>
        /// <param name="borders">框線旗標：左 1、右 2、上 4、下 8。</param>
        /// <param name="alignment">水平對齊方式。</param>
        /// <param name="vertical">垂直對齊方式。</param>
        /// <param name="wrap">是否啟用自動換行。</param>
        /// <param name="date">是否套用 Excel 內建日期格式。</param>
        /// <returns>已設定完成的儲存格樣式。</returns>
        private static ICellStyle Style(IWorkbook workbook, IFont font, int borders = 0,
            HorizontalAlignment alignment = HorizontalAlignment.General,
            VerticalAlignment vertical = VerticalAlignment.Center, bool wrap = false, bool date = false)
        {
            // 框線旗標：左 1、右 2、上 4、下 8。
            var style = workbook.CreateCellStyle();
            style.SetFont(font);
            style.Alignment = alignment;
            style.VerticalAlignment = vertical;
            style.WrapText = wrap;
            style.BorderLeft = (borders & 1) != 0 ? BorderStyle.Thin : BorderStyle.None;
            style.BorderRight = (borders & 2) != 0 ? BorderStyle.Thin : BorderStyle.None;
            style.BorderTop = (borders & 4) != 0 ? BorderStyle.Thin : BorderStyle.None;
            style.BorderBottom = (borders & 8) != 0 ? BorderStyle.Thin : BorderStyle.None;
            if ((borders & 1) != 0) style.LeftBorderColor = IndexedColors.Automatic.Index;
            if ((borders & 2) != 0) style.RightBorderColor = IndexedColors.Automatic.Index;
            if ((borders & 4) != 0) style.TopBorderColor = IndexedColors.Automatic.Index;
            if ((borders & 8) != 0) style.BottomBorderColor = IndexedColors.Automatic.Index;
            if (date) style.DataFormat = 14;
            return style;
        }

        /// <summary>
        /// 依欄位順序將格式索引套用至指定資料列。
        /// </summary>
        /// <param name="sheet">目標工作表。</param>
        /// <param name="row">從零起算的列索引。</param>
        /// <param name="styles">共用儲存格樣式陣列。</param>
        /// <param name="indices">各欄對應的樣式索引。</param>
        private static void ApplyStyles(ISheet sheet, int row, ICellStyle[] styles, int[] indices)
        {
            // 僅套用樣式，不改動儲存格內容。
            for (var column = 0; column < indices.Length; column++)
                sheet.GetRow(row).GetCell(column).CellStyle = styles[indices[column]];
        }

        /// <summary>
        /// 寫入固定文字並保留儲存格既有的樣式。
        /// </summary>
        /// <param name="sheet">目標工作表。</param>
        /// <param name="row">從零起算的列索引。</param>
        /// <param name="column">從零起算的欄索引。</param>
        /// <param name="value">待寫入的文字。</param>
        private static void Text(ISheet sheet, int row, int column, string value)
        {
            // 報告儲存格已由 CreateSheet 建立，直接填值即可保留格式。
            sheet.GetRow(row).GetCell(column).SetCellValue(value);
        }
    }
}
