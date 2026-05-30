using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.DownloadNoIncludeTax
{
    /// <summary>
    /// 建立空快稅金回桃園倉庫明細表，並避免載入與報表無關的資料。
    /// </summary>
    public class DownloadNoIncludeTaxService : _BaseService
    {
        private const string AirTranType = "空運";
        private const string AirSourceType = "3";
        private static readonly HashSet<string> ExcludedCompanies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "新瑞宅配",
            "新竹物流",
            "圓通自取"
        };

        public DownloadNoIncludeTaxService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 驗證下載條件、查詢報表資料，最後產生 Excel 檔案內容。
        /// </summary>
        public DownloadNoIncludeTaxExportResult Export(string source, string startDateText, string endDateText)
        {
            var result = new DownloadNoIncludeTaxExportResult();

            try
            {
                if (source != "1")
                {
                    throw new InvalidOperationException("資料來源錯誤，請重新選擇");
                }

                if (!DateTime.TryParse(startDateText, out var startDate) ||
                    !DateTime.TryParse(endDateText, out var endDate))
                {
                    throw new InvalidOperationException("日期格式錯誤，請重新選擇");
                }

                if (startDate.Date > endDate.Date)
                {
                    throw new InvalidOperationException("結束日期不可早於開始日期");
                }

                var startDateValue = startDate.ToString("yyyyMMdd");
                var endDateValue = endDate.ToString("yyyyMMdd");
                var rows = GetRows(startDateValue, endDateValue);

                result.Rows = rows;
                result.FileName = $"{startDateValue}~{endDateValue}-物流代收檔-空運-回桃園倉庫明細表-{rows.Count}票.xlsx";
                result.FileBytes = CreateWorkbookBytes(rows);
                result.status = Status.success;
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = GetInnermostExceptionMessage(ex);
            }

            return result;
        }

        /// <summary>
        /// 依日期區間查詢報表資料，並補齊客戶主檔與原始單資料。
        /// </summary>
        private List<DownloadNoIncludeTaxRow> GetRows(string startDate, string endDate)
        {
            // step1: 先縮小 FEE_MASTER 範圍，避免後續跨資料庫 lookup 收到不必要的 key。
            var feeMasters = JetfDb.FeeMasters
                .AsNoTracking()
                .Where(x =>
                    string.Compare(x.DataDate, startDate) >= 0 &&
                    string.Compare(x.DataDate, endDate) <= 0 &&
                    x.SourceType == AirSourceType &&
                    (x.IncludeTax == "N" || x.DlvCom == "40" || x.DlvCom == "41"))
                .Select(x => new FeeMasterReportRow
                {
                    Id = x.Id,
                    DataDate = x.DataDate,
                    Source = x.Source,
                    Type = x.Type,
                    Customer = x.Customer,
                    IncludeTax = x.IncludeTax,
                    BagNumber = x.BagNumber,
                    TrackingNo = x.TrackingNo,
                    Combine = x.Combine,
                    Tax1 = x.Tax1,
                    Tax2 = x.Tax2,
                    Ccfee = x.Ccfee,
                    Cod = x.Cod,
                    Fee = x.Fee,
                    ToDlvCod = x.ToDlvCod,
                    DlvCom = x.DlvCom,
                    Recipient = x.Recipient,
                    RecPhone = x.RecPhone,
                    InDateTime = x.InDateTime,
                    OutDateTime = x.OutDateTime
                })
                .ToList();

            if (!feeMasters.Any())
            {
                return new List<DownloadNoIncludeTaxRow>();
            }

            // step2: 查詢客戶主檔，排除不需要輸出的物流公司。
            var customerLookup = LoadCustomerLookup(feeMasters);
            var filteredRows = feeMasters
                .Select(x => new
                {
                    FeeMaster = x,
                    Customer = FindCustomer(customerLookup, x.Customer, x.DlvCom)
                })
                .Where(x => x.Customer != null && !ExcludedCompanies.Contains(x.Customer.Company ?? string.Empty))
                .ToList();

            // step3: 依剩餘貨號查詢 ORIGINALLIST，補上配送單號。
            var originalLookup = LoadOriginalLookup(filteredRows.Select(x => x.FeeMaster.TrackingNo));
            var reportRows = new List<DownloadNoIncludeTaxRow>();

            // step4: ORIGINALLIST 同一貨號可能有多筆，需保留原本 left join 展開後的結果。
            foreach (var item in filteredRows)
            {
                var originals = FindOriginals(originalLookup, item.FeeMaster.TrackingNo);
                if (!originals.Any())
                {
                    reportRows.Add(MapRow(item.FeeMaster, item.Customer, null));
                    continue;
                }

                foreach (var original in originals)
                {
                    reportRows.Add(MapRow(item.FeeMaster, item.Customer, original));
                }
            }

            return reportRows
                .OrderBy(x => x.Source)
                .ThenBy(x => x.Customer)
                .ThenBy(x => x.DataDate)
                .ThenBy(x => x.FeeMasterId)
                .ThenBy(x => x.OriginalListId)
                .ToList();
        }

        /// <summary>
        /// 依客戶代號與派件代碼查詢 customer_master，建立報表用 lookup。
        /// </summary>
        private Dictionary<string, CustomerReportRow> LoadCustomerLookup(IEnumerable<FeeMasterReportRow> feeMasters)
        {
            var keys = feeMasters
                .Select(x => new CustomerLookupKey
                {
                    CustId = PadLeft(x.Customer, 5),
                    TransNo = x.DlvCom
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.CustId) && !string.IsNullOrWhiteSpace(x.TransNo))
                .GroupBy(x => BuildKey(x.CustId, x.TransNo))
                .Select(x => x.First())
                .ToList();
            var lookup = new Dictionary<string, CustomerReportRow>();

            if (!keys.Any())
            {
                return lookup;
            }

            // 使用 temp table join，避免日期區間較大時超過 SQL Server 參數數量限制。
            var customers = JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => x.TranType == AirTranType)
                .WhereBulkContains(
                    JetfDb,
                    keys,
                    row => new { row.CustId, row.TransNo },
                    key => new { key.CustId, key.TransNo })
                .Select(x => new CustomerReportRow
                {
                    Id = x.Id,
                    CustId = x.CustId,
                    TransNo = x.TransNo,
                    Customer = x.Customer,
                    TransName = x.TransName,
                    Company = x.Company
                })
                .ToList();

            foreach (var customer in customers.OrderBy(x => x.Id))
            {
                var key = BuildKey(customer.CustId, customer.TransNo);
                if (!lookup.ContainsKey(key))
                {
                    lookup.Add(key, customer);
                }
            }

            return lookup;
        }

        /// <summary>
        /// 依貨號查詢 ORIGINALLIST，建立一個貨號對應多筆原始單的 lookup。
        /// </summary>
        private Dictionary<string, List<OriginalReportRow>> LoadOriginalLookup(IEnumerable<string> trackingNos)
        {
            var lookup = new Dictionary<string, List<OriginalReportRow>>();
            var keys = trackingNos
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!keys.Any())
            {
                return lookup;
            }

            // 使用 temp table join，一次載入資料且不將大量貨號展開成 IN 參數。
            var originals = DataCenterDb.OriginalLists
                .AsNoTracking()
                .WhereBulkContains(
                    DataCenterDb,
                    keys,
                    row => row.TrackingNo,
                    key => key)
                .Select(x => new OriginalReportRow
                {
                    Id = x.Id,
                    TrackingNo = x.TrackingNo,
                    DeliveryNo = x.DeliveryNo
                })
                .ToList();

            foreach (var original in originals)
            {
                if (!lookup.TryGetValue(original.TrackingNo ?? string.Empty, out var rows))
                {
                    rows = new List<OriginalReportRow>();
                    lookup.Add(original.TrackingNo ?? string.Empty, rows);
                }

                rows.Add(original);
            }

            return lookup;
        }

        /// <summary>
        /// 依客戶代號與派件代碼尋找對應的客戶主檔。
        /// </summary>
        private static CustomerReportRow FindCustomer(
            IReadOnlyDictionary<string, CustomerReportRow> lookup,
            string customer,
            string transNo)
        {
            lookup.TryGetValue(BuildKey(PadLeft(customer, 5), transNo), out var value);
            return value;
        }

        /// <summary>
        /// 依貨號取得所有原始單；找不到時回傳空集合，維持 left join 行為。
        /// </summary>
        private static List<OriginalReportRow> FindOriginals(
            IReadOnlyDictionary<string, List<OriginalReportRow>> lookup,
            string trackingNo)
        {
            return lookup.TryGetValue(trackingNo ?? string.Empty, out var rows)
                ? rows
                : new List<OriginalReportRow>();
        }

        /// <summary>
        /// 將資料庫查詢結果轉成 Excel 報表列。
        /// </summary>
        private static DownloadNoIncludeTaxRow MapRow(
            FeeMasterReportRow feeMaster,
            CustomerReportRow customer,
            OriginalReportRow original)
        {
            return new DownloadNoIncludeTaxRow
            {
                FeeMasterId = feeMaster.Id,
                OriginalListId = original?.Id ?? 0,
                DataDate = feeMaster.DataDate,
                Source = feeMaster.Source,
                Type = feeMaster.Type,
                Customer = customer.Customer,
                IncludeTax = feeMaster.IncludeTax,
                BagNumber = feeMaster.BagNumber,
                TrackingNo = feeMaster.TrackingNo,
                InDateTime = feeMaster.InDateTime,
                OutDateTime = feeMaster.OutDateTime,
                Recipient = feeMaster.Recipient,
                RecPhone = feeMaster.RecPhone,
                Combine = feeMaster.Combine,
                Tax1 = feeMaster.Tax1 ?? 0,
                Tax2 = feeMaster.Tax2 ?? 0,
                Ccfee = feeMaster.Ccfee ?? 0,
                Cod = feeMaster.Cod ?? 0,
                Fee = feeMaster.Fee ?? 0,
                ToDlvCod = ParseInt(feeMaster.ToDlvCod),
                TransName = customer.TransName,
                DeliveryNo = original?.DeliveryNo
            };
        }

        /// <summary>
        /// 依客戶分頁建立 Excel workbook，並轉成可放入 TempData 的 byte array。
        /// </summary>
        private static byte[] CreateWorkbookBytes(IReadOnlyList<DownloadNoIncludeTaxRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in rows.GroupBy(x => x.Customer ?? string.Empty))
            {
                CreateSheet(workbook, CreateUniqueSheetName(group.Key, usedSheetNames), group.ToList());
            }

            if (!rows.Any())
            {
                CreateSheet(workbook, "無資料", new List<DownloadNoIncludeTaxRow>());
            }

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 建立單一客戶的 Excel sheet。
        /// </summary>
        private static void CreateSheet(IWorkbook workbook, string sheetName, IReadOnlyList<DownloadNoIncludeTaxRow> rows)
        {
            var dateStyle = workbook.CreateCellStyle();
            dateStyle.DataFormat = workbook.CreateDataFormat().GetFormat("yyyy/mm/dd hh:mm:ss");

            var sheet = workbook.CreateSheet(sheetName);
            var header = sheet.CreateRow(0);
            var titles = new[]
            {
                "項次", "作業日", "來源", "報關類型", "客戶名稱", "是否包稅", "清關袋號", "分提單號",
                "入倉時間", "出倉時間", "姓名", "電話", "併單", "稅金1", "稅金2", "報關費",
                "到付款", "手續費", "代收貨款金額", "派件公司", "物流單號"
            };

            for (var column = 0; column < titles.Length; column++)
            {
                header.CreateCell(column).SetCellValue(titles[column]);
                sheet.SetColumnWidth(column, column == 0 || column == 1 || column == 2 || column == 3 ||
                    column == 5 || column == 12 || column == 13 || column == 14 || column == 15 ||
                    column == 16 || column == 17 ? 3000 : 6000);
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var item = rows[index];
                var row = sheet.CreateRow(index + 1);
                row.CreateCell(0).SetCellValue(index + 1);
                row.CreateCell(1).SetCellValue(item.DataDate ?? string.Empty);
                row.CreateCell(2).SetCellValue(item.Source ?? string.Empty);
                row.CreateCell(3).SetCellValue(item.Type ?? string.Empty);
                row.CreateCell(4).SetCellValue(item.Customer ?? string.Empty);
                row.CreateCell(5).SetCellValue(item.IncludeTax ?? string.Empty);
                row.CreateCell(6).SetCellValue(item.BagNumber ?? string.Empty);
                row.CreateCell(7).SetCellValue(item.TrackingNo ?? string.Empty);
                SetDateCell(row, 8, item.InDateTime, dateStyle);
                SetDateCell(row, 9, item.OutDateTime, dateStyle);
                row.CreateCell(10).SetCellValue(item.Recipient ?? string.Empty);
                row.CreateCell(11).SetCellValue(item.RecPhone ?? string.Empty);
                row.CreateCell(12).SetCellValue(item.Combine ?? string.Empty);
                row.CreateCell(13).SetCellValue(item.Tax1);
                row.CreateCell(14).SetCellValue(item.Tax2);
                row.CreateCell(15).SetCellValue(item.Ccfee);
                row.CreateCell(16).SetCellValue(item.Cod);
                row.CreateCell(17).SetCellValue(item.Fee);
                row.CreateCell(18).SetCellValue(item.ToDlvCod);
                row.CreateCell(19).SetCellValue(item.TransName ?? string.Empty);
                row.CreateCell(20).SetCellValue(item.DeliveryNo ?? string.Empty);
            }
        }

        /// <summary>
        /// 日期有值時才建立儲存格，避免空日期被輸出成預設值。
        /// </summary>
        private static void SetDateCell(IRow row, int column, DateTime? value, ICellStyle dateStyle)
        {
            if (!value.HasValue)
            {
                return;
            }

            var cell = row.CreateCell(column);
            cell.SetCellValue(value.Value);
            cell.CellStyle = dateStyle;
        }

        /// <summary>
        /// 產生符合 Excel 限制且不重複的 sheet 名稱。
        /// </summary>
        private static string CreateUniqueSheetName(string value, ISet<string> usedNames)
        {
            var invalidCharacters = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var safeName = string.IsNullOrWhiteSpace(value) ? "未命名客戶" : value.Trim();
            foreach (var character in invalidCharacters)
            {
                safeName = safeName.Replace(character, '_');
            }

            safeName = safeName.Length > 31 ? safeName.Substring(0, 31) : safeName;
            var candidate = safeName;
            var suffix = 1;
            while (!usedNames.Add(candidate))
            {
                var suffixText = $"_{suffix++}";
                candidate = safeName.Substring(0, Math.Min(safeName.Length, 31 - suffixText.Length)) + suffixText;
            }

            return candidate;
        }

        private static string PadLeft(string value, int length)
        {
            return (value ?? string.Empty).PadLeft(length, '0');
        }

        private static string BuildKey(string first, string second)
        {
            return $"{first ?? string.Empty}\u001f{second ?? string.Empty}";
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out var number) ? number : 0;
        }

        /// <summary>
        /// 取得最內層例外訊息，避免前端只看到 EF 外層包裝訊息。
        /// </summary>
        private static string GetInnermostExceptionMessage(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        private sealed class CustomerLookupKey
        {
            public string CustId { get; set; }
            public string TransNo { get; set; }
        }

        private sealed class FeeMasterReportRow
        {
            public int Id { get; set; }
            public string DataDate { get; set; }
            public string Source { get; set; }
            public string Type { get; set; }
            public string Customer { get; set; }
            public string IncludeTax { get; set; }
            public string BagNumber { get; set; }
            public string TrackingNo { get; set; }
            public string Combine { get; set; }
            public int? Tax1 { get; set; }
            public int? Tax2 { get; set; }
            public int? Ccfee { get; set; }
            public int? Cod { get; set; }
            public int? Fee { get; set; }
            public string ToDlvCod { get; set; }
            public string DlvCom { get; set; }
            public string Recipient { get; set; }
            public string RecPhone { get; set; }
            public DateTime? InDateTime { get; set; }
            public DateTime? OutDateTime { get; set; }
        }

        private sealed class CustomerReportRow
        {
            public int Id { get; set; }
            public string CustId { get; set; }
            public string TransNo { get; set; }
            public string Customer { get; set; }
            public string TransName { get; set; }
            public string Company { get; set; }
        }

        private sealed class OriginalReportRow
        {
            public int Id { get; set; }
            public string TrackingNo { get; set; }
            public string DeliveryNo { get; set; }
        }
    }

    public sealed class DownloadNoIncludeTaxExportResult
    {
        public DownloadNoIncludeTaxExportResult()
        {
            Rows = new List<DownloadNoIncludeTaxRow>();
            status = Status.success;
            msg = string.Empty;
        }

        public string status { get; set; }
        public string msg { get; set; }
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public List<DownloadNoIncludeTaxRow> Rows { get; set; }
    }

    public sealed class DownloadNoIncludeTaxRow
    {
        public int FeeMasterId { get; set; }
        public int OriginalListId { get; set; }
        public string DataDate { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string Customer { get; set; }
        public string IncludeTax { get; set; }
        public string BagNumber { get; set; }
        public string TrackingNo { get; set; }
        public DateTime? InDateTime { get; set; }
        public DateTime? OutDateTime { get; set; }
        public string Recipient { get; set; }
        public string RecPhone { get; set; }
        public string Combine { get; set; }
        public int Tax1 { get; set; }
        public int Tax2 { get; set; }
        public int Ccfee { get; set; }
        public int Cod { get; set; }
        public int Fee { get; set; }
        public int ToDlvCod { get; set; }
        public string TransName { get; set; }
        public string DeliveryNo { get; set; }
    }
}
