using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ReconciliationCustomerSelection;
using Service.Services.ReconciliationCustomerSelection.Domain;
using Service.Services.Receivable.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.Receivable
{
    /// <summary>
    /// 應收未收明細查詢與匯出服務。
    /// </summary>
    public sealed class ReceivableService : _BaseService
    {
        private readonly ReconciliationCustomerSelectionService _customerSelectionService;

        /// <summary>
        /// 建立應收未收明細服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        /// <param name="customerSelectionService">共用客戶選擇服務。</param>
        public ReceivableService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext,
            ReconciliationCustomerSelectionService customerSelectionService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _customerSelectionService = customerSelectionService;
        }

        /// <summary>
        /// 分頁查詢應收未收明細。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>分頁明細。</returns>
        public ReceivableQueryResponse Search(ReceivableQueryRequest request)
        {
            var page = request != null && request.Page > 0 ? request.Page : 1;
            var pageSize = request != null && request.PageSize > 0 ? request.PageSize : 20;
            pageSize = Math.Min(pageSize, 200);

            var query = BuildQuery(request);
            var totalCount = query.Count();
            var rows = ProjectRows(query
                    .OrderByDescending(x => x.FeeMaster.OutDateTime)
                    .ThenByDescending(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                .ToList();

            return new ReceivableQueryResponse
            {
                TotalCount = totalCount,
                Data = BuildListItems(rows)
            };
        }

        /// <summary>
        /// 匯出符合條件的全部應收未收明細。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>Excel 檔案內容。</returns>
        public byte[] ExportExcel(ReceivableQueryRequest request)
        {
            var rows = ProjectRows(BuildQuery(request)
                    .OrderByDescending(x => x.FeeMaster.OutDateTime)
                    .ThenByDescending(x => x.Id))
                .ToList();
            var data = BuildListItems(rows);

            // 先取得本次匯出客戶所屬的群組，後續依群組或個別客戶建立頁籤。
            var customerGroupNames = GetCustomerGroupNames(
                data.Select(x => x.CustomerCode));
            var workbook = CreateExcelWorkbook(data, customerGroupNames);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 取得海運、空運客戶及客戶群組選項。
        /// </summary>
        /// <returns>客戶選擇彈窗資料。</returns>
        public ReconciliationCustomerSelectionOptions GetCustomerSelectionOptions()
        {
            return _customerSelectionService.GetOptions();
        }

        /// <summary>
        /// 建立應收未收明細的資料庫查詢。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>尚未執行的費用明細查詢。</returns>
        private IQueryable<FeeMasterDetailEntity> BuildQuery(ReceivableQueryRequest request)
        {
            var startDate = ParseDate(request?.OutDateStart, "開始日期");
            var endDate = ParseDate(request?.OutDateEnd, "結束日期");
            if (!startDate.HasValue || !endDate.HasValue)
            {
                throw new ArgumentException("日期為必填，請選擇開始日期與結束日期。");
            }

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                throw new ArgumentException("開始日期不可晚於結束日期。");
            }

            var endDateExclusive = endDate?.AddDays(1);
            var customerCodes = (request?.CustomerCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var trackingNo = request?.TrackingNo?.Trim();
            var dlvInv = request?.DlvInv?.Trim();

            ValidateEnum(request?.Status, "狀態");
            ValidateEnum(request?.CollectionType, "區分");

            var query = JetfDb.FeeMasterDetails
                .AsNoTracking()
                .Where(x => x.FeeMaster.Download == "1")
                .WhereIf(startDate.HasValue,
                    x => x.FeeMaster.OutDateTime.HasValue && x.FeeMaster.OutDateTime >= startDate.Value)
                .WhereIf(endDateExclusive.HasValue,
                    x => x.FeeMaster.OutDateTime.HasValue && x.FeeMaster.OutDateTime < endDateExclusive.Value)
                .WhereIf(customerCodes.Any(), x => customerCodes.Contains(x.FeeMaster.Customer))
                // 有輸入單號時才套用精確比對，避免空白條件影響查詢結果。
                .WhereIf(!string.IsNullOrWhiteSpace(trackingNo),
                    x => x.TrackingNo == trackingNo)
                .WhereIf(!string.IsNullOrWhiteSpace(dlvInv),
                    x => x.DlvInv == dlvInv)
                .WhereIf(request?.Status == ReceivableStatus.Received,
                    x => x.ReceivedCustomerCodTime.HasValue || x.ReceivedToDlvCodTime.HasValue)
                .WhereIf(request?.Status == ReceivableStatus.Unreceived,
                    x => !x.ReceivedCustomerCodTime.HasValue && !x.ReceivedToDlvCodTime.HasValue)
                .WhereIf(request?.CollectionType == ReceivableCollectionType.Customer,
                    x => (x.CustomerCod ?? 0) > 0)
                .WhereIf(request?.CollectionType == ReceivableCollectionType.Trans,
                    x => !string.IsNullOrEmpty(x.ToDlvCod) &&
                         x.ToDlvCod != "0");

            return query;
        }

        /// <summary>
        /// 將費用明細查詢投影為應收未收資料列。
        /// </summary>
        /// <param name="query">費用明細查詢。</param>
        /// <returns>尚未執行的應收未收資料列查詢。</returns>
        private static IQueryable<ReceivableDataRow> ProjectRows(
            IQueryable<FeeMasterDetailEntity> query)
        {
            return query.Select(x => new ReceivableDataRow
            {
                Id = x.Id,
                OutDateTime = x.FeeMaster.OutDateTime,
                CustomerReconciliationDate = x.ReceivedCustomerCodTime,
                LogisticsReconciliationDate = x.ReconciliationLogisticsId.HasValue
                    ? (DateTime?)x.ReconciliationLogistics.RepaymentDate
                    : null,
                Source = x.FeeMaster.Source,
                Type = x.FeeMaster.Type,
                CustomerCode = x.FeeMaster.Customer,
                TrackingNo = x.TrackingNo,
                DlvInv = x.DlvInv,
                TaxNumber = x.TaxNumber,
                Ccfee = x.Ccfee ?? 0,
                Cod = x.Cod ?? 0,
                Fee = x.Fee ?? 0,
                CustomerCod = x.CustomerCod ?? 0,
                TransCod = x.TransCod ?? 0,
                ToDlvCod = x.ToDlvCod,
                ReceivedCustomerCod = x.ReceivedCustomerCod ?? 0,
                ReceivedToDlvCod = x.ReceivedToDlvCod ?? 0
            });
        }

        /// <summary>
        /// 將資料庫投影資料轉換為畫面及匯出使用的明細。
        /// </summary>
        /// <param name="rows">資料庫投影資料。</param>
        /// <returns>包含客戶名稱及計算金額的應收未收明細。</returns>
        private List<ReceivableListItem> BuildListItems(List<ReceivableDataRow> rows)
        {
            var customerCodes = rows
                .Select(x => x.CustomerCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var customerNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            AddCustomerNames(customerNames, GetSeaCustomerNames(customerCodes));
            AddCustomerNames(customerNames, GetAirCustomerNames(customerCodes));

            return rows.Select(row =>
            {
                var transCod = row.TransCod;
                var codSubtotal = row.CustomerCod + row.ToDlvCod.ToInt();
                var receivedAmount = row.ReceivedCustomerCod + row.ReceivedToDlvCod;
                string customerName;
                customerNames.TryGetValue(row.CustomerCode ?? string.Empty, out customerName);

                return new ReceivableListItem
                {
                    Id = row.Id,
                    PostingDate = row.OutDateTime?.ToString("yyyy/MM/dd"),
                    CustomerReconciliationDate = row.CustomerReconciliationDate?.ToString("yyyy/MM/dd"),
                    LogisticsReconciliationDate = row.LogisticsReconciliationDate?.ToString("yyyy/MM/dd"),
                    Source = row.Source,
                    Type = row.Type,
                    CustomerCode = row.CustomerCode,
                    CustomerName = string.IsNullOrWhiteSpace(customerName)
                        ? row.CustomerCode
                        : customerName,
                    OutDateTime = row.OutDateTime?.ToString("yyyy/MM/dd HH:mm:ss"),
                    TrackingNo = row.TrackingNo,
                    DlvInv = row.DlvInv,
                    TaxNumber = row.TaxNumber,
                    CodSubtotal = codSubtotal,
                    ReceivedAmount = receivedAmount,
                    UnreceivedAmount = codSubtotal - receivedAmount,
                    CustomerCod = row.CustomerCod,
                    TransCod = transCod,
                    JetfPayment = string.Empty,
                    Ccfee = row.Ccfee,
                    RedispatchFreight = string.Empty,
                    Cod = row.Cod,
                    Fee = row.Fee,
                    UnreceivedReason = string.Empty
                };
            }).ToList();
        }

        /// <summary>
        /// 取得客戶代號所屬的客戶群組名稱。
        /// </summary>
        /// <param name="customerCodes">匯出資料包含的客戶代號。</param>
        /// <returns>以客戶代號為鍵、群組名稱為值的對照表。</returns>
        private Dictionary<string, string> GetCustomerGroupNames(IEnumerable<string> customerCodes)
        {
            var codes = customerCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!codes.Any())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return JetfDb.ReconciliationCustomerGroupDetails
                .AsNoTracking()
                // 僅查詢本次匯出資料包含的客戶，避免載入不需要的群組明細。
                .Where(x => codes.Contains(x.CustCode))
                // 透過 Navigation Property 取得群組名稱，不需手動撰寫 join。
                .Select(x => new
                {
                    x.CustCode,
                    x.CustomerGroup.GroupName
                })
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode) &&
                            !string.IsNullOrWhiteSpace(x.GroupName))
                .GroupBy(x => x.CustCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().GroupName.Trim(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 將客戶名稱加入目標字典，已存在的客戶代號不覆蓋。
        /// </summary>
        /// <param name="target">合併後的客戶名稱字典。</param>
        /// <param name="source">要加入的客戶名稱字典。</param>
        private static void AddCustomerNames(
            IDictionary<string, string> target,
            IDictionary<string, string> source)
        {
            foreach (var item in source)
            {
                if (!target.ContainsKey(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                {
                    target.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// 解析 yyyy-MM-dd 格式的日期字串。
        /// </summary>
        /// <param name="value">日期字串。</param>
        /// <param name="fieldName">錯誤訊息使用的欄位名稱。</param>
        /// <returns>解析後的日期；空白字串回傳 null。</returns>
        private static DateTime? ParseDate(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTime result;
            if (!DateTime.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
            {
                throw new ArgumentException($"{fieldName}格式錯誤，請使用 yyyy-MM-dd。");
            }

            return result.Date;
        }

        /// <summary>
        /// 驗證可空列舉值是否為已定義的選項。
        /// </summary>
        /// <typeparam name="TEnum">列舉類型。</typeparam>
        /// <param name="value">要驗證的列舉值。</param>
        /// <param name="fieldName">錯誤訊息使用的欄位名稱。</param>
        private static void ValidateEnum<TEnum>(TEnum? value, string fieldName)
            where TEnum : struct
        {
            if (value.HasValue && !Enum.IsDefined(typeof(TEnum), value.Value))
            {
                throw new ArgumentException($"{fieldName}選項不正確。");
            }
        }

        /// <summary>
        /// 建立應收未收明細 Excel 活頁簿。
        /// </summary>
        /// <param name="data">要匯出的應收未收明細。</param>
        /// <param name="customerGroupNames">客戶代號與群組名稱對照表。</param>
        /// <returns>完成欄位、資料及格式設定的 Excel 活頁簿。</returns>
        private static IWorkbook CreateExcelWorkbook(
            List<ReceivableListItem> data,
            IDictionary<string, string> customerGroupNames)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);
            var headers = new[]
            {
                "序號", "掛帳日", "客戶銷帳日", "物流銷帳日", "資料來源", "報關類別", "客戶", "出倉時間",
                "分提單號", "物流貨號", "稅單號碼", "代收小計", "已收金額", "未收金額",
                "跟廠商收", "跟派件收", "捷豐支付", "報關費", "重派運費", "到付款",
                "手續費", "未回收原因"
            };

            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sheetGroups = data
                .GroupBy(item =>
                {
                    string groupName;

                    // 已加入群組的客戶共用群組鍵；未加入群組的客戶仍以代號區分，避免同名客戶被合併。
                    return customerGroupNames.TryGetValue(
                        item.CustomerCode ?? string.Empty,
                        out groupName)
                        ? $"GROUP:{groupName}"
                        : $"CUSTOMER:{item.CustomerCode ?? string.Empty}";
                })
                .OrderBy(group => group.Key)
                .ToList();

            foreach (var group in sheetGroups)
            {
                var firstItem = group.First();
                string groupName;

                // 群組資料使用群組名稱作為頁籤名稱，未分組資料使用轉換後的客戶中文名稱。
                var sheetName = customerGroupNames.TryGetValue(
                    firstItem.CustomerCode ?? string.Empty,
                    out groupName)
                    ? groupName
                    : firstItem.CustomerName;

                CreateExcelSheet(
                    workbook,
                    CreateUniqueSheetName(sheetName, usedSheetNames),
                    group.ToList(),
                    headers,
                    headerStyle,
                    dataStyle,
                    numberStyle);
            }

            if (!sheetGroups.Any())
            {
                // NPOI 活頁簿至少需要一個頁籤，查無資料時建立空白頁籤。
                CreateExcelSheet(
                    workbook,
                    "無資料",
                    new List<ReceivableListItem>(),
                    headers,
                    headerStyle,
                    dataStyle,
                    numberStyle);
            }

            return workbook;
        }

        /// <summary>
        /// 建立單一客戶或客戶群組的 Excel 頁籤。
        /// </summary>
        /// <param name="workbook">Excel 活頁簿。</param>
        /// <param name="sheetName">頁籤名稱。</param>
        /// <param name="data">頁籤內的應收未收明細。</param>
        /// <param name="headers">欄位標題。</param>
        /// <param name="headerStyle">標題儲存格樣式。</param>
        /// <param name="dataStyle">一般資料儲存格樣式。</param>
        /// <param name="numberStyle">金額儲存格樣式。</param>
        private static void CreateExcelSheet(
            IWorkbook workbook,
            string sheetName,
            IReadOnlyList<ReceivableListItem> data,
            string[] headers,
            ICellStyle headerStyle,
            ICellStyle dataStyle,
            ICellStyle numberStyle)
        {
            var sheet = workbook.CreateSheet(sheetName);
            NpoiCell.CreateHeaderCells(sheet.CreateRow(0), headers, headerStyle);
            for (var index = 0; index < data.Count; index++)
            {
                var item = data[index];
                var row = sheet.CreateRow(index + 1);
                var column = 0;

                NpoiCell.CreateIntCell(row, column++, index + 1, dataStyle);
                NpoiCell.CreateCell(row, column++, item.PostingDate, dataStyle);
                NpoiCell.CreateCell(row, column++, item.CustomerReconciliationDate, dataStyle);
                NpoiCell.CreateCell(row, column++, item.LogisticsReconciliationDate, dataStyle);
                NpoiCell.CreateCell(row, column++, item.Source, dataStyle);
                NpoiCell.CreateCell(row, column++, item.Type, dataStyle);
                NpoiCell.CreateCell(row, column++, item.CustomerName, dataStyle);
                NpoiCell.CreateCell(row, column++, item.OutDateTime, dataStyle);
                NpoiCell.CreateCell(row, column++, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, column++, item.DlvInv, dataStyle);
                NpoiCell.CreateCell(row, column++, item.TaxNumber, dataStyle);
                NpoiCell.CreateIntCell(row, column++, item.CodSubtotal, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.ReceivedAmount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.UnreceivedAmount, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.CustomerCod, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.TransCod, numberStyle);
                NpoiCell.CreateCell(row, column++, item.JetfPayment, dataStyle);
                NpoiCell.CreateIntCell(row, column++, item.Ccfee, numberStyle);
                NpoiCell.CreateCell(row, column++, item.RedispatchFreight, dataStyle);
                NpoiCell.CreateIntCell(row, column++, item.Cod, numberStyle);
                NpoiCell.CreateIntCell(row, column++, item.Fee, numberStyle);
                NpoiCell.CreateCell(row, column, item.UnreceivedReason, dataStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 12);
        }

        /// <summary>
        /// 產生符合 Excel 限制且不重複的頁籤名稱。
        /// </summary>
        /// <param name="value">原始頁籤名稱。</param>
        /// <param name="usedNames">已使用的頁籤名稱。</param>
        /// <returns>可安全建立且不重複的頁籤名稱。</returns>
        private static string CreateUniqueSheetName(string value, ISet<string> usedNames)
        {
            var invalidCharacters = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var safeName = string.IsNullOrWhiteSpace(value) ? "未指定客戶" : value.Trim();

            // Excel 頁籤不可包含特定符號，且名稱長度不可超過 31 個字元。
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
                candidate = safeName.Substring(
                    0,
                    Math.Min(safeName.Length, 31 - suffixText.Length)) + suffixText;
            }

            return candidate;
        }
    }
}
