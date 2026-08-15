using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ReceivableCod.Domain;
using Service.Services.ReconciliationCustomerSelection;
using Service.Services.ReconciliationCustomerSelection.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.ReceivableCod
{
    /// <summary>
    /// 到付款應收未收明細查詢與匯出服務。
    /// </summary>
    public sealed class ReceivableCodService : _BaseService
    {
        private readonly ReconciliationCustomerSelectionService _customerSelectionService;

        /// <summary>
        /// 建立到付款應收未收明細服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        /// <param name="customerSelectionService">共用客戶選擇服務。</param>
        public ReceivableCodService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext,
            ReconciliationCustomerSelectionService customerSelectionService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _customerSelectionService = customerSelectionService;
        }

        /// <summary>
        /// 分頁查詢到付款應收未收明細。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>分頁明細。</returns>
        public ReceivableCodQueryResponse Search(ReceivableCodQueryRequest request)
        {
            var page = request != null && request.Page > 0 ? request.Page : 1;
            var pageSize = request != null && request.PageSize > 0 ? request.PageSize : 20;
            pageSize = Math.Min(pageSize, 200);

            var query = BuildQuery(request);
            var totalCount = query.Count();
            var rows = ProjectRows(query
                    .OrderByDescending(x => x.SignOutTime)
                    .ThenByDescending(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                .ToList();

            return new ReceivableCodQueryResponse
            {
                TotalCount = totalCount,
                Data = BuildListItems(rows)
            };
        }

        /// <summary>
        /// 匯出符合條件的全部到付款應收未收明細。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>Excel 檔案內容。</returns>
        public byte[] ExportExcel(ReceivableCodQueryRequest request)
        {
            var rows = ProjectRows(BuildQuery(request)
                    .OrderByDescending(x => x.SignOutTime)
                    .ThenByDescending(x => x.Id))
                .ToList();
            var workbook = CreateExcelWorkbook(BuildListItems(rows));

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
        /// 建立到付款應收未收明細的資料庫查詢。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>尚未執行的到付款資料查詢。</returns>
        private IQueryable<FeeMasterCodEntity> BuildQuery(ReceivableCodQueryRequest request)
        {
            var startDate = ParseDate(request?.SignOutDateStart, "開始日期");
            var endDate = ParseDate(request?.SignOutDateEnd, "結束日期");
            if (!startDate.HasValue || !endDate.HasValue)
            {
                throw new ArgumentException("日期為必填，請選擇開始日期與結束日期。");
            }

            if (startDate.Value > endDate.Value)
            {
                throw new ArgumentException("開始日期不可晚於結束日期。");
            }

            var endDateExclusive = endDate.Value.AddDays(1);
            var customerCodes = (request?.CustomerCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var trackingNo = request?.TrackingNo?.Trim();
            var dlvInv = request?.DlvInv?.Trim();

            ValidateEnum(request?.Status, "狀態");

            return JetfDb.FeeMasterCods
                .AsNoTracking()
                .Where(x => x.SignOutTime >= startDate.Value &&
                            x.SignOutTime < endDateExclusive)
                // 未選擇客戶時查詢全部；有選擇時只查指定客戶。
                .WhereIf(customerCodes.Any(), x => customerCodes.Contains(x.Customer))
                // 有輸入單號時才套用精確比對，避免空白條件影響查詢結果。
                .WhereIf(!string.IsNullOrWhiteSpace(trackingNo),
                    x => x.TrackingNo == trackingNo)
                .WhereIf(!string.IsNullOrWhiteSpace(dlvInv),
                    x => x.DlvInv == dlvInv)
                // 已收回與未收回皆以銷帳時間是否存在判斷。
                .WhereIf(request?.Status == ReceivableStatus.Received,
                    x => x.ReceivedCcTime.HasValue)
                .WhereIf(request?.Status == ReceivableStatus.Unreceived,
                    x => !x.ReceivedCcTime.HasValue);
        }

        /// <summary>
        /// 將到付款資料查詢投影為畫面需要的欄位。
        /// </summary>
        /// <param name="query">到付款資料查詢。</param>
        /// <returns>尚未執行的資料列查詢。</returns>
        private static IQueryable<ReceivableCodDataRow> ProjectRows(
            IQueryable<FeeMasterCodEntity> query)
        {
            return query.Select(x => new ReceivableCodDataRow
            {
                Id = x.Id,
                // SOURCE_TYPE 移除後，依排程相同規則由 DATA_TYPE 區分空運與海運。
                CustomerType = x.DataType == "tact" || x.DataType == "ftz"
                    ? "AIR"
                    : "SEA",
                DataType = x.DataType,
                CustomerCode = x.Customer,
                SignOutTime = x.SignOutTime,
                TrackingNo = x.TrackingNo,
                DlvInv = x.DlvInv,
                CodAmount = x.Cc,
                FreightFee = x.FreightFee ?? 0,
                Fee = x.Fee ?? 0,
                ReceivableAmount = x.ToDlvCod ?? 0,
                ReceivedAmount = x.ReceivedCc ?? 0
            });
        }

        /// <summary>
        /// 將資料庫投影資料轉換為包含客戶中文名稱及計算金額的明細。
        /// </summary>
        /// <param name="rows">資料庫投影資料。</param>
        /// <returns>畫面及匯出使用的明細。</returns>
        private List<ReceivableCodListItem> BuildListItems(
            IReadOnlyCollection<ReceivableCodDataRow> rows)
        {
            var airType = CustomerType.AIR.ToString();
            var seaType = CustomerType.SEA.ToString();
            var airCustomerCodes = rows
                .Where(x => string.Equals(
                    x.CustomerType,
                    airType,
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.CustomerCode);
            var seaCustomerCodes = rows
                .Where(x => string.Equals(
                    x.CustomerType,
                    seaType,
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.CustomerCode);

            // AIR 與 SEA 的客戶代號來源不同，分開取得名稱以避免同代號互相覆蓋。
            var airCustomerNames = new Dictionary<string, string>(
                GetAirCustomerNames(airCustomerCodes),
                StringComparer.OrdinalIgnoreCase);
            var seaCustomerNames = new Dictionary<string, string>(
                GetSeaCustomerNames(seaCustomerCodes),
                StringComparer.OrdinalIgnoreCase);

            return rows.Select(row =>
            {
                string customerName = null;
                if (string.Equals(
                    row.CustomerType,
                    airType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    airCustomerNames.TryGetValue(
                        row.CustomerCode ?? string.Empty,
                        out customerName);
                }
                else if (string.Equals(
                    row.CustomerType,
                    seaType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    seaCustomerNames.TryGetValue(
                        row.CustomerCode ?? string.Empty,
                        out customerName);
                }

                return new ReceivableCodListItem
                {
                    Id = row.Id,
                    PostingDate = row.SignOutTime.ToString("yyyy/MM/dd"),
                    Source = GetSourceName(row.CustomerType),
                    Type = row.DataType,
                    CustomerCode = row.CustomerCode,
                    CustomerName = string.IsNullOrWhiteSpace(customerName)
                        ? row.CustomerCode
                        : customerName,
                    OutDateTime = row.SignOutTime.ToString("yyyy/MM/dd HH:mm:ss"),
                    TrackingNo = row.TrackingNo,
                    DlvInv = row.DlvInv,
                    CodAmount = row.CodAmount,
                    FreightFee = row.FreightFee,
                    Fee = row.Fee,
                    ReceivableAmount = row.ReceivableAmount,
                    ReceivedAmount = row.ReceivedAmount,
                    UnreceivedAmount = row.ReceivableAmount - row.ReceivedAmount
                };
            }).ToList();
        }

        /// <summary>
        /// 將 AIR、SEA 來源代碼轉換為畫面顯示名稱。
        /// </summary>
        /// <param name="customerType">客戶類型代碼。</param>
        /// <returns>來源類型顯示名稱。</returns>
        private static string GetSourceName(string customerType)
        {
            CustomerType parsedCustomerType;
            return Enum.TryParse(customerType, true, out parsedCustomerType)
                ? parsedCustomerType.ToDescription()
                : customerType;
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
        /// 建立到付款應收未收明細 Excel 活頁簿。
        /// </summary>
        /// <param name="data">要匯出的明細。</param>
        /// <returns>完成欄位、資料及格式設定的 Excel 活頁簿。</returns>
        private static IWorkbook CreateExcelWorkbook(
            IReadOnlyList<ReceivableCodListItem> data)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("到付款應收未收明細");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);
            var headers = new[]
            {
                "序號", "掛帳日", "資料來源", "報關類別", "客戶", "出倉時間",
                "分提單號", "物流貨號", "到付款", "運費", "手續費",
                "應收金額", "已收金額", "未收金額"
            };

            NpoiCell.CreateHeaderCells(sheet.CreateRow(0), headers, headerStyle);
            for (var index = 0; index < data.Count; index++)
            {
                var item = data[index];
                var row = sheet.CreateRow(index + 1);
                var column = 0;

                NpoiCell.CreateIntCell(row, column++, index + 1, dataStyle);
                NpoiCell.CreateCell(row, column++, item.PostingDate, dataStyle);
                NpoiCell.CreateCell(row, column++, item.Source, dataStyle);
                NpoiCell.CreateCell(row, column++, item.Type, dataStyle);
                NpoiCell.CreateCell(row, column++, item.CustomerName, dataStyle);
                NpoiCell.CreateCell(row, column++, item.OutDateTime, dataStyle);
                NpoiCell.CreateCell(row, column++, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, column++, item.DlvInv, dataStyle);
                NpoiCell.CreateDoubleCell(
                    row,
                    column++,
                    Convert.ToDouble(item.CodAmount),
                    numberStyle);
                NpoiCell.CreateDoubleCell(
                    row,
                    column++,
                    Convert.ToDouble(item.FreightFee),
                    numberStyle);
                NpoiCell.CreateDoubleCell(
                    row,
                    column++,
                    Convert.ToDouble(item.Fee),
                    numberStyle);
                NpoiCell.CreateDoubleCell(
                    row,
                    column++,
                    Convert.ToDouble(item.ReceivableAmount),
                    numberStyle);
                NpoiCell.CreateDoubleCell(
                    row,
                    column++,
                    Convert.ToDouble(item.ReceivedAmount),
                    numberStyle);
                NpoiCell.CreateDoubleCell(
                    row,
                    column,
                    Convert.ToDouble(item.UnreceivedAmount),
                    numberStyle);
            }

            sheet.AutoSizeColumns(headers.Length, scale: 1.2, minWidth: 12);
            return workbook;
        }
    }
}
