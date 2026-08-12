using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ReconciliationCustomerSelection;
using Service.Services.ReconciliationCustomerSelection.Domain;
using Service.Services.ReconciliationIncludeTaxDownload.Domain;
using Service.Services.ReconciliationIncludeTaxFormat;
using Service.Services.ReconciliationIncludeTaxFormat.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Service.Services.ReconciliationIncludeTaxDownload
{
    /// <summary>
    /// 包稅客戶明細查詢與 Excel 下載服務。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadService : _BaseService
    {
        private readonly ReconciliationIncludeTaxFormatService _formatService;
        private readonly ReconciliationCustomerSelectionService _customerSelectionService;

        /// <summary>
        /// 建立包稅客戶明細下載服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">資料中心資料庫內容。</param>
        /// <param name="formatService">包稅客戶匯出格式服務。</param>
        /// <param name="customerSelectionService">共用客戶選擇服務。</param>
        public ReconciliationIncludeTaxDownloadService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext,
            ReconciliationIncludeTaxFormatService formatService,
            ReconciliationCustomerSelectionService customerSelectionService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _formatService = formatService;
            _customerSelectionService = customerSelectionService;
        }

        /// <summary>
        /// 取得可供下載使用的包稅客戶匯出格式。
        /// </summary>
        /// <returns>格式清單。</returns>
        public List<ReconciliationIncludeTaxFormatListItem> GetFormats()
        {
            return _formatService.Search();
        }

        /// <summary>
        /// 取得海運、空運客戶及客戶群組選項。
        /// </summary>
        /// <returns>客戶選擇資料。</returns>
        public ReconciliationCustomerSelectionOptions GetCustomerSelectionOptions()
        {
            return _customerSelectionService.GetOptions();
        }

        /// <summary>
        /// 依查詢條件建立包稅客戶明細下載檔案。 
        /// </summary>
        /// <param name="request">下載查詢條件。</param>
        /// <returns>Excel 或 ZIP 檔案下載結果。</returns>
        public ReconciliationIncludeTaxDownloadExportResult Export(
            ReconciliationIncludeTaxDownloadRequest request)
        {
            ValidateRequest(request);
            var startDate = DateTime.Parse(request.OutDateStart).Date;
            var endDate = DateTime.Parse(request.OutDateEnd).Date;
            if (startDate > endDate)
            {
                throw new ArgumentException("開始日期不可晚於結束日期。");
            }

            // Step 1：依日期、客戶及包稅註記查詢符合條件的費用明細。
            var rows = QueryRows(request, startDate, endDate);
            // Step 2：以客戶群組為優先合併資料；未加入群組的客戶各自建立檔案。
            var fileGroups = BuildFileGroups(rows);
            if (fileGroups.Count <= 1)
            {
                var singleRows = fileGroups.Count == 1 ? fileGroups[0].Rows : rows;
                return new ReconciliationIncludeTaxDownloadExportResult
                {
                    FileBytes = CreateExcelBytes(singleRows, request.FormatId),
                    FileName = CreateExcelFileName(fileGroups.Count == 1 ? fileGroups[0].Name : "明細")
                };
            }

            // Step 3：多個客戶／群組各自產生 Excel，合併為單一 ZIP 供下載。
            return new ReconciliationIncludeTaxDownloadExportResult
            {
                FileBytes = CreateZipBytes(fileGroups, request.FormatId),
                FileName = $"包稅客戶明細_{DateTime.Now:yyyyMMddHHmmss}.zip"
            };
        }

        /// <summary>
        /// 將查詢資料依客戶群組分檔；同一群組的客戶共用一個檔案。
        /// </summary>
        /// <param name="rows">查詢到的包稅客戶明細。</param>
        /// <returns>待輸出的檔案群組。</returns>
        private List<ReconciliationIncludeTaxDownloadFileGroup> BuildFileGroups(
            IReadOnlyList<ReconciliationIncludeTaxDownloadRow> rows)
        {
            var exportRows = rows ?? new List<ReconciliationIncludeTaxDownloadRow>();
            var customerCodes = exportRows
                .Select(x => x.Customer)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var customerGroups = GetCustomerGroups(customerCodes);
            var customerNames = GetCustomerNames(customerCodes);

            // 將匯出資料的客戶代號轉成客戶名稱，Excel 的客戶欄位顯示名稱。
            foreach (var row in exportRows)
            {
                var customerCode = (row.Customer ?? string.Empty).Trim();
                row.CustomerName = customerNames.TryGetValue(customerCode, out var customerName)
                    ? customerName
                    : customerCode;
            }

            return exportRows
                .GroupBy(row => GetFileGroupKey(row.Customer, customerGroups), StringComparer.OrdinalIgnoreCase)
                .Select(group => new ReconciliationIncludeTaxDownloadFileGroup
                {
                    Name = GetFileGroupName(
                        group.Key,
                        group.First().Customer,
                        customerGroups,
                        customerNames),
                    Rows = group.ToList()
                })
                .ToList();
        }

        /// <summary>
        /// 取得客戶代號對應的客戶名稱。
        /// </summary>
        /// <param name="customerCodes">客戶代號。</param>
        /// <returns>以客戶代號索引的客戶名稱。</returns>
        private Dictionary<string, string> GetCustomerNames(
            IReadOnlyCollection<string> customerCodes)
        {
            var customerNames = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            AddCustomerNames(customerNames, GetSeaCustomerNames(customerCodes));
            AddCustomerNames(customerNames, GetAirCustomerNames(customerCodes));
            return customerNames;
        }

        /// <summary>
        /// 將客戶名稱加入對照表，已存在的客戶代號不覆蓋。
        /// </summary>
        /// <param name="target">目標客戶名稱對照表。</param>
        /// <param name="source">來源客戶名稱對照表。</param>
        private static void AddCustomerNames(
            IDictionary<string, string> target,
            IDictionary<string, string> source)
        {
            foreach (var item in source)
            {
                if (!target.ContainsKey(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                {
                    target.Add(item.Key, item.Value.Trim());
                }
            }
        }

        /// <summary>
        /// 查詢客戶所屬的客戶群組。
        /// </summary>
        /// <param name="customerCodes">客戶代號。</param>
        /// <returns>以客戶代號索引的客戶群組。</returns>
        private Dictionary<string, ReconciliationIncludeTaxDownloadCustomerGroup> GetCustomerGroups(
            IReadOnlyCollection<string> customerCodes)
        {
            if (customerCodes == null || customerCodes.Count == 0)
            {
                return new Dictionary<string, ReconciliationIncludeTaxDownloadCustomerGroup>(
                    StringComparer.OrdinalIgnoreCase);
            }

            return JetfDb.ReconciliationCustomerGroupDetails
                .AsNoTracking()
                .Where(x => customerCodes.Contains(x.CustCode))
                .Select(x => new
                {
                    x.CustCode,
                    GroupId = x.CustomerGroupId,
                    GroupName = x.CustomerGroup.GroupName
                })
                .ToList()
                .GroupBy(x => (x.CustCode ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => new ReconciliationIncludeTaxDownloadCustomerGroup
                    {
                        GroupId = group.First().GroupId,
                        GroupName = group.First().GroupName
                    },
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得檔案分組索引。群組使用群組 Id，避免不同群組同名時被合併。
        /// </summary>
        private static string GetFileGroupKey(
            string customerCode,
            IDictionary<string, ReconciliationIncludeTaxDownloadCustomerGroup> customerGroups)
        {
            ReconciliationIncludeTaxDownloadCustomerGroup customerGroup;
            var normalizedCustomerCode = (customerCode ?? string.Empty).Trim();
            if (customerGroups.TryGetValue(normalizedCustomerCode, out customerGroup))
            {
                return $"G:{customerGroup.GroupId}";
            }

            return $"C:{normalizedCustomerCode}";
        }

        /// <summary>
        /// 取得檔案分組名稱。
        /// </summary>
        private static string GetFileGroupName(
            string groupKey,
            string customerCode,
            IDictionary<string, ReconciliationIncludeTaxDownloadCustomerGroup> customerGroups,
            IDictionary<string, string> customerNames)
        {
            if (groupKey.StartsWith("G:", StringComparison.OrdinalIgnoreCase))
            {
                var groupId = int.Parse(groupKey.Substring(2));
                var customerGroup = customerGroups.Values.FirstOrDefault(x => x.GroupId == groupId);
                if (customerGroup != null && !string.IsNullOrWhiteSpace(customerGroup.GroupName))
                {
                    return customerGroup.GroupName;
                }
            }

            string customerName;
            var normalizedCustomerCode = (customerCode ?? string.Empty).Trim();
            return customerNames.TryGetValue(normalizedCustomerCode, out customerName)
                ? customerName
                : (string.IsNullOrWhiteSpace(normalizedCustomerCode)
                    ? "未指定客戶"
                    : normalizedCustomerCode);
        }

        /// <summary>
        /// 建立單一 Excel 檔案的位元組內容。
        /// </summary>
        private byte[] CreateExcelBytes(
            IReadOnlyList<ReconciliationIncludeTaxDownloadRow> rows,
            int formatId)
        {
            var workbook = CreateExcelWorkbook(rows, formatId);
            using (var stream = new MemoryStream())
            {
                try
                {
                    workbook.Write(stream);
                    return stream.ToArray();
                }
                finally
                {
                    workbook.Close();
                }
            }
        }

        /// <summary>
        /// 將多個客戶 Excel 檔案壓縮成 ZIP。
        /// </summary>
        private byte[] CreateZipBytes(
            IReadOnlyList<ReconciliationIncludeTaxDownloadFileGroup> fileGroups,
            int formatId)
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var fileGroup in fileGroups)
                    {
                        var fileName = CreateUniqueExcelFileName(fileGroup.Name, usedFileNames);
                        var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                        var fileBytes = CreateExcelBytes(fileGroup.Rows, formatId);
                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// 建立單一 Excel 下載檔名。
        /// </summary>
        private static string CreateExcelFileName(string name)
        {
            return $"包稅客戶明細_{SanitizeFileName(name)}.xlsx";
        }

        /// <summary>
        /// 建立 ZIP 內不重複的 Excel 檔名。
        /// </summary>
        private static string CreateUniqueExcelFileName(
            string name,
            ISet<string> usedFileNames)
        {
            var baseName = $"包稅客戶明細_{SanitizeFileName(name)}";
            var fileName = $"{baseName}.xlsx";
            var suffix = 2;
            while (!usedFileNames.Add(fileName))
            {
                fileName = $"{baseName}_{suffix++}.xlsx";
            }

            return fileName;
        }

        /// <summary>
        /// 移除 Windows 檔名不允許的字元。
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var value = string.IsNullOrWhiteSpace(name) ? "未指定客戶" : name.Trim();
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            value = value.Trim().TrimEnd('.');
            if (value.Length == 0)
            {
                return "未指定客戶";
            }

            return value.Substring(0, Math.Min(value.Length, 80));
        }

        /// <summary>
        /// 建立符合格式設定的 Excel 活頁簿。
        /// </summary>
        /// <param name="rows">FEE_MASTER／FEE_MASTER_DETAIL 查詢資料。</param>
        /// <param name="formatId">匯出格式識別碼。</param>
        /// <returns>Excel 活頁簿。</returns>
        public IWorkbook CreateExcelWorkbook(
            IReadOnlyList<ReconciliationIncludeTaxDownloadRow> rows,
            int formatId)
        {
            var format = _formatService.GetDetail(formatId);
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("明細");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            NpoiCell.CreateHeaderCells(
                sheet.CreateRow(0),
                format.Columns.Select(x => x.ColumnName).ToList(),
                headerStyle);

            var exportRows = rows ?? new List<ReconciliationIncludeTaxDownloadRow>();
            for (var rowIndex = 0; rowIndex < exportRows.Count; rowIndex++)
            {
                var excelRow = sheet.CreateRow(rowIndex + 1);
                var dataRow = exportRows[rowIndex];
                for (var columnIndex = 0; columnIndex < format.Columns.Count; columnIndex++)
                {
                    var column = format.Columns[columnIndex];
                    var value = column.SourceType == ReconciliationIncludeTaxColumnSourceType.Constant
                        ? column.DefaultValue
                        : GetFieldValue(dataRow, column.FieldKey);
                    CreateExcelCell(excelRow, columnIndex, value, dataStyle);
                }
            }

            sheet.AutoSizeColumns(format.Columns.Count, minWidth: 12);
            return workbook;
        }

        /// <summary>
        /// 查詢 Download=1 且 INCLUDE_TAX 為 C 的費用明細。
        /// </summary>
        /// <param name="request">下載查詢條件。</param>
        /// <param name="startDate">開始日期。</param>
        /// <param name="endDate">結束日期。</param>
        /// <returns>供 Excel 套用格式的資料列。</returns>
        private List<ReconciliationIncludeTaxDownloadRow> QueryRows(
            ReconciliationIncludeTaxDownloadRequest request,
            DateTime startDate,
            DateTime endDate)
        {
            var customerCodes = (request.CustomerCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var endDateExclusive = endDate.AddDays(1);

            var rows = JetfDb.FeeMasters
                .AsNoTracking()
                .Where(x => x.Download == "1")
                .Where(x => x.IncludeTax == "C")
                .Where(x => x.OutDateTime.HasValue &&
                            x.OutDateTime >= startDate &&
                            x.OutDateTime < endDateExclusive)
                .WhereIf(customerCodes.Any(), x => customerCodes.Contains(x.Customer))
                .SelectMany(master => master.Details.Select(detail => new ReconciliationIncludeTaxDownloadRow
                {
                    OutDateTime = master.OutDateTime,
                    Source = master.Source,
                    Type = master.Type,
                    Customer = master.Customer,
                    TaxNumber = detail.TaxNumber,
                    MainNumber = detail.MainNumber,
                    BagNumber = detail.BagNumber,
                    ClearanceNumber = detail.ClearanceNumber,
                    TrackingNo = detail.TrackingNo,
                    DlvInv = detail.DlvInv,
                    TaxPayer = detail.TaxPayer,
                    OriginalTaxPayer = detail.Recipient,
                    Tax = detail.Tax,
                    TaxBase = detail.TaxBase,
                    Ccfee = detail.Ccfee
                }))
                .OrderBy(x => x.OutDateTime)
                .ThenBy(x => x.Customer)
                .ThenBy(x => x.TrackingNo)
                .ToList();

            SetReconciliationAirTaxes(rows);
            return rows;
        }

        /// <summary>
        /// 批次查詢 TACT／FTZ 分提單號對應的空快銷帳稅額並回填匯出資料。
        /// </summary>
        /// <param name="rows">包稅客戶明細下載資料。</param>
        private void SetReconciliationAirTaxes(
            IReadOnlyCollection<ReconciliationIncludeTaxDownloadRow> rows)
        {
            var airRows = rows
                .Where(x => IsReconciliationAirSource(x.Source))
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo) ||
                            !string.IsNullOrWhiteSpace(x.BagNumber))
                .ToList();
            if (!airRows.Any())
            {
                return;
            }

            // Step 1：先使用費用明細的分提單號批次查詢空快銷帳資料。
            var trackingNos = airRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .Select(x => x.TrackingNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var trackingReconciliationAirs = JetfDb.ReconciliationAirs
                .AsNoTracking()
                .WhereBulkContains(JetfDb, trackingNos, x => x.TrackingNo, x => x);

            // Step 2：以分提單號建立索引，找不到的資料才改用清關袋號再次比對。
            var reconciliationAirByTrackingNo = trackingReconciliationAirs
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

            // Step 2-1：只保留分提單號查不到、但有清關袋號的資料，準備進行備援比對。
            var fallbackBagNumbers = airRows
                .Where(x => !string.IsNullOrWhiteSpace(x.BagNumber))
                .Where(x => string.IsNullOrWhiteSpace(x.TrackingNo) ||
                            !reconciliationAirByTrackingNo.ContainsKey(x.TrackingNo.Trim()))
                .Select(x => x.BagNumber.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Step 2-2：使用清關袋號批次查詢 ReconciliationAir 的 TrackingNo。
            var bagReconciliationAirs = JetfDb.ReconciliationAirs
                .AsNoTracking()
                .WhereBulkContains(JetfDb, fallbackBagNumbers, x => x.TrackingNo, x => x);

            // Step 2-3：建立清關袋號對照表，供後續回填資料時快速取得比對結果。
            var reconciliationAirByBagNumber = bagReconciliationAirs
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

            // Step 3：將找到的稅額及納稅義務人回填至每筆匯出資料。
            foreach (var row in airRows)
            {
                ReconciliationAirEntity reconciliationAir = null;
                var matched = !string.IsNullOrWhiteSpace(row.TrackingNo) &&
                              reconciliationAirByTrackingNo.TryGetValue(
                                  row.TrackingNo.Trim(),
                                  out reconciliationAir);
                if (!matched && !string.IsNullOrWhiteSpace(row.BagNumber))
                {
                    matched = reconciliationAirByBagNumber.TryGetValue(
                        row.BagNumber.Trim(),
                        out reconciliationAir);
                }

                if (!matched)
                {
                    continue;
                }

                row.BusinessTax = reconciliationAir.BusinessTax;
                row.ImportTax = reconciliationAir.ImportTax;
                row.TaxPayer = reconciliationAir.Recipient;
            }
        }

        /// <summary>
        /// 判斷費用主檔資料來源是否為 TACT 或 FTZ。
        /// </summary>
        /// <param name="source">費用主檔資料來源。</param>
        /// <returns>是否需查詢空快代收銷帳稅額。</returns>
        private static bool IsReconciliationAirSource(string source)
        {
            var value = (source ?? string.Empty).Trim();
            return string.Equals(value, "TACT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "FTZ", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依欄位 enum 讀取明細 Model 的匯出值；金額欄位保留數值型別。
        /// </summary>
        /// <param name="row">明細資料列。</param>
        /// <param name="fieldKey">格式欄位代碼。</param>
        /// <returns>匯出值。</returns>
        private static object GetFieldValue(
            ReconciliationIncludeTaxDownloadRow row,
            string fieldKey)
        {
            ReconciliationIncludeTaxField field;
            if (row == null || !ReconciliationIncludeTaxFieldExtensions.TryParseFieldKey(fieldKey, out field))
            {
                return string.Empty;
            }

            switch (field)
            {
                case ReconciliationIncludeTaxField.FeeMaster_OutDateTime:
                    return row.OutDateTime.HasValue ? row.OutDateTime.Value.ToString("yyyy/MM/dd") : string.Empty;
                case ReconciliationIncludeTaxField.FeeMaster_Source:
                    return row.Source ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMaster_Type:
                    return row.Type ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMaster_Customer:
                    return row.CustomerName ?? row.Customer ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TaxNumber:
                    return row.TaxNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_MainNumber:
                    return row.MainNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_BagNumber:
                    return row.BagNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_ClearanceNumber:
                    return row.ClearanceNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TrackingNo:
                    return row.TrackingNo ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_DlvInv:
                    return row.DlvInv ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TaxPayer:
                    return row.TaxPayer ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_Recipient:
                    return row.OriginalTaxPayer ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_Tax:
                    return row.Tax;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TaxBase:
                    return row.TaxBase;
                case ReconciliationIncludeTaxField.FeeMasterDetail_Ccfee:
                    return row.Ccfee;
                case ReconciliationIncludeTaxField.ReconciliationAir_BusinessTax:
                    return row.BusinessTax;
                case ReconciliationIncludeTaxField.ReconciliationAir_ImportTax:
                    return row.ImportTax;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 依值的型別建立 Excel 儲存格，避免金額欄位被寫成文字。
        /// </summary>
        /// <param name="row">Excel 資料列。</param>
        /// <param name="columnIndex">欄位索引。</param>
        /// <param name="value">儲存格值。</param>
        /// <param name="style">儲存格樣式。</param>
        private static void CreateExcelCell(
            IRow row,
            int columnIndex,
            object value,
            ICellStyle style)
        {
            if (value is int)
            {
                NpoiCell.CreateIntCell(row, columnIndex, (int)value, style);
                return;
            }

            NpoiCell.CreateCell(row, columnIndex, value as string ?? string.Empty, style);
        }

        /// <summary>
        /// 驗證下載必要條件。
        /// </summary>
        /// <param name="request">下載查詢條件。</param>
        private static void ValidateRequest(ReconciliationIncludeTaxDownloadRequest request)
        {
            DateTime value;
            if (request == null || !DateTime.TryParse(request.OutDateStart, out value))
            {
                throw new ArgumentException("日期為必填，請選擇開始日期。");
            }

            if (!DateTime.TryParse(request.OutDateEnd, out value))
            {
                throw new ArgumentException("日期為必填，請選擇結束日期。");
            }

            if (request.FormatId <= 0)
            {
                throw new ArgumentException("請選擇匯出格式。");
            }
        }
    }
}
