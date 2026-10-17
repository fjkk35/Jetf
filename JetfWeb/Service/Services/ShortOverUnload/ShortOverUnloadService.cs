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
        /// 驗證資料來源與匯入內容、補齊資料並產生短卸報告。
        /// </summary>
        /// <param name="input">上傳 Excel 的資料串流。</param>
        /// <param name="format">短溢卸資料來源，目前支援台中-短卸。</param>
        /// <param name="importedAt">本次匯入的台北日期時間。</param>
        /// <returns>可供下載的短卸報告 Excel 位元組。</returns>
        public async Task<byte[]> GenerateAsync(Stream input, ShortOverUnloadDataSource format, DateTime importedAt)
        {
            // 先驗證資料來源及 Excel，避免無效檔案觸發資料庫或 API 查詢。
            if (format != ShortOverUnloadDataSource.TaichungShortUnload)
                throw new InvalidDataException("請選擇台中-短卸。");

            var rows = ReadExcel(input);
            var vessels = new Dictionary<string, ShortOverUnloadVessel>();
            var vesselCache = new Dictionary<string, ShortOverUnloadVessel>();

            // 相同主提單的資料集中補值；不同主提單若使用相同船掛則共用 API 結果。
            foreach (var group in rows.GroupBy(row => row.MainNumber))
            {
                var registration = LoadOrderData(group.Key, group.ToList());
                if (!vesselCache.TryGetValue(registration, out var vessel))
                {
                    vessel = await LoadVesselAsync(registration);
                    vesselCache.Add(registration, vessel);
                }
                vessels.Add(group.Key, vessel);
            }

            // 補值完成後統一依主提單及每頁 14 筆的規則產生報告。
            return Export(rows, vessels, importedAt);
        }

        /// <summary>
        /// 使用 EF 取得船掛，依主提單及匯入分提單補齊裝貨型態、貨物說明及櫃號，多筆資料取第一筆。
        /// </summary>
        /// <param name="mainNumber">主提單號碼。</param>
        /// <param name="rows">待補值的主提單明細。</param>
        /// <returns>供 GB330、GB325 查詢使用的船掛。</returns>
        private string LoadOrderData(string mainNumber, List<ShortOverUnloadRow> rows)
        {
            // 船掛取查詢結果的第一筆，只在查無船掛或第一筆為空白時停止匯出。
            var registration = DataCenterDb.CesMainOrders
                .AsNoTracking()
                .Where(order => order.MainNumber == mainNumber && order.Type == "O")
                .Select(order => order.FieldA == null || order.FieldA.Trim() == ""
                    ? null
                    : order.FieldA.Trim())
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(registration))
                throw new InvalidDataException($"主提單「{mainNumber}」的 CES_MAIN_ORDER（TYPE=O）查無船掛，請確認資料。");

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
                // 字典已保留各分提單的第一筆資料，查不到鍵時提示原始匯入位置。
                var location = $"工作表「{row.SourceSheet}」第 {row.SourceRow} 列（{mainNumber} / {row.BlNo}）";
                if (!manifests.TryGetValue(row.BlNo, out var manifest))
                    throw new InvalidDataException($"{location}：SEA_MANIFEST_UPLOAD 查無裝貨型態。");
                if (!items.TryGetValue(row.BlNo, out var item))
                    throw new InvalidDataException($"{location}：SEA_ORDER_EDIT 查無 ITEM_NO=1。");
                row.Manifest = manifest.Manifest?.Trim();
                row.ItemName = item.ItemName;
                row.ContainerNo = item.ContainerNo;
            }
            return registration.Trim();
        }

        /// <summary>
        /// 透過 GB330 與 GB325 取得報告表頭所需的船舶資料，符合船掛的資料取第一筆。
        /// </summary>
        /// <param name="registration">海關通關號碼（船掛）。</param>
        /// <returns>船舶代碼、航次、船名、受理關別及預定到港日期。</returns>
        private async Task<ShortOverUnloadVessel> LoadVesselAsync(string registration)
        {
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

            if (gb330 == null || !IsSuccessful(gb330.Status, gb330.Msg))
                throw new InvalidDataException($"船掛「{registration}」GB330 查詢失敗：{gb330?.Msg}");

            // 明細優先取第一筆符合船掛的資料；沒有明細時相容直接回傳表頭欄位的格式。
            var match = gb330.GridModel?
                .FirstOrDefault(row => string.Equals(row.VslRegNo?.Trim(), registration, StringComparison.OrdinalIgnoreCase));
            var vessel = new ShortOverUnloadVessel { VslRegNo = registration };
            if (match != null)
            {
                vessel.ImoNo = match.ImoNo;
                vessel.VoyageFlightNo = match.VoyageFlightNo;
                vessel.ShipName = match.VslName;
            }
            else if ((gb330.GridModel == null || gb330.GridModel.Count == 0)
                && string.Equals(gb330.VslRegNo?.Trim(), registration, StringComparison.OrdinalIgnoreCase))
            {
                vessel.ImoNo = gb330.ImoNo;
                vessel.VoyageFlightNo = gb330.VoyageFlightNo;
                vessel.ShipName = gb330.ShipName;
            }
            else
            {
                throw new InvalidDataException($"船掛「{registration}」GB330 查無符合資料，請確認船掛。");
            }

            if (string.IsNullOrWhiteSpace(vessel.ImoNo) || string.IsNullOrWhiteSpace(vessel.VoyageFlightNo)
                || string.IsNullOrWhiteSpace(vessel.ShipName))
                throw new InvalidDataException($"船掛「{registration}」GB330 缺少船（機）代碼、船舶航次或船名。");

            // GB325 使用相同船掛查詢受理關別與預定到港日。
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
            if (gb325 == null || !IsSuccessful(gb325.Status, gb325.Msg))
                throw new InvalidDataException($"船掛「{registration}」GB325 查詢失敗：{gb325?.Msg}");

            // 確認 API 回傳船掛一致，並將到港日轉成真正的日期值供 Excel 使用。
            if ((!string.IsNullOrWhiteSpace(gb325.VslRegNo)
                    && !string.Equals(gb325.VslRegNo.Trim(), registration, StringComparison.OrdinalIgnoreCase))
                || string.IsNullOrWhiteSpace(gb325.ProcessCustCd)
                || !DateTime.TryParseExact(gb325.EstArDate?.Trim(),
                    new[] { "yyyyMMdd", "yyyy/M/d", "yyyy-MM-dd" }, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var arrivalDate))
                throw new InvalidDataException($"船掛「{registration}」GB325 缺少有效受理關別、預定到港日期或回傳船掛不符。");

            vessel.ProcessCustCd = gb325.ProcessCustCd;
            vessel.EstimatedArrivalDate = arrivalDate;
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

        /// <summary>
        /// 讀取各非空白頁籤，檢查欄名、提單號碼並轉換件數。
        /// </summary>
        /// <param name="input">上傳 Excel 的資料串流。</param>
        /// <returns>保留來源頁籤、列號與匯入順序的短溢卸明細。</returns>
        private static List<ShortOverUnloadRow> ReadExcel(Stream input)
        {
            // 每個資料頁籤都使用相同四欄表頭，來源資訊保留給後續錯誤提示。
            var headers = new[] { "主提單號碼", "分提單號碼", "申報", "實到" };
            var result = new List<ShortOverUnloadRow>();
            var workbook = WorkbookFactory.Create(input);
            try
            {
                var formatter = new DataFormatter(CultureInfo.InvariantCulture);
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
                        var values = Enumerable.Range(0, 4)
                            .Select(column => ReadText(row?.GetCell(column), formatter, evaluator)).ToArray();
                        // 完全空白的資料列略過，部分填寫的列仍需接受完整欄位檢查。
                        if (values.All(string.IsNullOrWhiteSpace))
                            continue;

                        var location = $"工作表「{sheet.SheetName}」第 {rowIndex + 1} 列";
                        if (string.IsNullOrWhiteSpace(values[0]) || string.IsNullOrWhiteSpace(values[1]))
                            throw new InvalidDataException($"{location}：主提單號碼及分提單號碼不可空白。");

                        result.Add(new ShortOverUnloadRow
                        {
                            SourceSheet = sheet.SheetName,
                            SourceRow = rowIndex + 1,
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
                return WriteWorkbook(workbook);
            }
            finally
            {
                workbook.Close();
            }
        }

        /// <summary>
        /// 依主提單分組，每 14 筆建立一張符合參考檔格式的報告。
        /// </summary>
        /// <param name="rows">已補齊資料的匯入明細。</param>
        /// <param name="vessels">以主提單號碼索引的船舶資料。</param>
        /// <param name="importedAt">本次匯入的台北日期時間。</param>
        /// <returns>包含各主提單分頁的短卸報告 Excel 位元組。</returns>
        private static byte[] Export(List<ShortOverUnloadRow> rows,
            IDictionary<string, ShortOverUnloadVessel> vessels, DateTime importedAt)
        {
            // 第 8～21 列共可容納 14 筆，超過時建立下一個頁籤。
            const int rowsPerSheet = 14;
            if (rows == null || rows.Count == 0)
                throw new InvalidDataException("沒有可匯出的短溢卸明細。");

            var workbook = new XSSFWorkbook();
            try
            {
                var sheetNames = new HashSet<string>();
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
                        var sheetName = CreateSheetName(group.Key, page, sheetNames);
                        var sheet = CreateSheet(workbook, sheetName, styles);

                        // 依指定儲存格填入業者代碼、船舶資料、卸存地點及日期。
                        Cell(sheet, 2, 3).SetCellValue("24951752");
                        Cell(sheet, 2, 4).SetCellValue(vessel.ImoNo);
                        Cell(sheet, 2, 6).SetCellValue(vessel.VoyageFlightNo);
                        Cell(sheet, 2, 7).SetCellValue(vessel.ProcessCustCd);
                        SetDate(workbook, Cell(sheet, 4, 2), vessel.EstimatedArrivalDate, dateStyles);
                        Cell(sheet, 4, 3).SetCellValue(vessel.ShipName);
                        Cell(sheet, 4, 4).SetCellValue(vessel.VslRegNo);
                        SetDate(workbook, Cell(sheet, 4, 6), importedAt, dateStyles);
                        Cell(sheet, 4, 7).SetCellValue("TXG018DH");
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
                            if (!string.IsNullOrEmpty(detail.ItemName))
                                Cell(sheet, 7 + line, 5).SetCellValue(detail.ItemName);
                            Cell(sheet, 7 + line, 6).SetCellValue("短卸");
                            if (!string.IsNullOrEmpty(detail.ContainerNo))
                                Cell(sheet, 7 + line, 7).SetCellValue(detail.ContainerNo);
                        }
                    }
                }

                // 開啟報告時先顯示第一頁，再序列化為可下載的檔案。
                workbook.SetActiveSheet(0);
                return WriteWorkbook(workbook);
            }
            finally
            {
                workbook.Close();
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
        /// <param name="date">待寫入的日期時間。</param>
        /// <param name="styles">以原始樣式索引為鍵的日期樣式快取。</param>
        private static void SetDate(IWorkbook workbook, ICell cell, DateTime date,
            IDictionary<short, ICellStyle> styles)
        {
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
            cell.SetCellValue(date.Date);
        }

        /// <summary>
        /// 依主提單及頁碼產生合法且不重複的 Excel 頁籤名稱。
        /// </summary>
        /// <param name="mainNumber">主提單號碼。</param>
        /// <param name="page">從一開始的分頁序號。</param>
        /// <param name="usedNames">本次匯出已使用的頁籤名稱集合。</param>
        /// <returns>不超過 31 個字元且已加入名稱集合的頁籤名稱。</returns>
        private static string CreateSheetName(string mainNumber, int page, ISet<string> usedNames)
        {
            // 替換 Excel 禁用字元，並為頁碼及撞名序號預留長度。
            var safeName = WorkbookUtil.CreateSafeSheetName(mainNumber);
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
