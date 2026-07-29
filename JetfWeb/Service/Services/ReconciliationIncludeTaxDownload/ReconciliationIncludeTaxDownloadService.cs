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

            return exportRows
                .GroupBy(row => GetFileGroupKey(row.Customer, customerGroups), StringComparer.OrdinalIgnoreCase)
                .Select(group => new ReconciliationIncludeTaxDownloadFileGroup
                {
                    Name = GetFileGroupName(group.Key, group.First().Customer, customerGroups),
                    Rows = group.ToList()
                })
                .ToList();
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
            IDictionary<string, ReconciliationIncludeTaxDownloadCustomerGroup> customerGroups)
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

            return string.IsNullOrWhiteSpace(customerCode) ? "未指定客戶" : customerCode.Trim();
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
                    NpoiCell.CreateCell(excelRow, columnIndex, value, dataStyle);
                }
            }

            sheet.AutoSizeColumns(format.Columns.Count, minWidth: 12);
            return workbook;
        }

        /// <summary>
        /// 查詢 Download=1 且 INCLUDE_TAX 為 C 或 Y 的費用明細。
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
                .Where(x => x.IncludeTax == "C" || x.IncludeTax == "Y")
                .Where(x => x.OutDateTime.HasValue &&
                            x.OutDateTime >= startDate &&
                            x.OutDateTime < endDateExclusive)
                .WhereIf(customerCodes.Any(), x => customerCodes.Contains(x.Customer))
                .SelectMany(master => master.Details.Select(detail => new ReconciliationIncludeTaxDownloadRow
                {
                    OutDateTime = master.OutDateTime,
                    Type = master.Type,
                    Customer = master.Customer,
                    TaxNumber = detail.TaxNumber,
                    MainNumber = detail.MainNumber,
                    BagNumber = detail.BagNumber,
                    TrackingNo = detail.TrackingNo,
                    DlvInv = detail.DlvInv,
                    TaxPayer = detail.TaxPayer,
                    Tax = detail.Tax,
                    TaxBase = detail.TaxBase
                }))
                .OrderBy(x => x.OutDateTime)
                .ThenBy(x => x.Customer)
                .ThenBy(x => x.TrackingNo)
                .ToList();
            return rows;
        }

        /// <summary>
        /// 依欄位 enum 讀取明細 Model 的匯出值。
        /// </summary>
        /// <param name="row">明細資料列。</param>
        /// <param name="fieldKey">格式欄位代碼。</param>
        /// <returns>匯出文字。</returns>
        private static string GetFieldValue(
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
                case ReconciliationIncludeTaxField.FeeMaster_Type:
                    return row.Type ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMaster_Customer:
                    return row.Customer ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TaxNumber:
                    return row.TaxNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_MainNumber:
                    return row.MainNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_BagNumber:
                    return row.BagNumber ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TrackingNo:
                    return row.TrackingNo ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_DlvInv:
                    return row.DlvInv ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_TaxPayer:
                    return row.TaxPayer ?? string.Empty;
                case ReconciliationIncludeTaxField.FeeMasterDetail_Tax:
                    return (row.Tax ?? 0).ToString();
                case ReconciliationIncludeTaxField.FeeMasterDetail_TaxBase:
                    return (row.TaxBase ?? 0).ToString();
                default:
                    return string.Empty;
            }
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
