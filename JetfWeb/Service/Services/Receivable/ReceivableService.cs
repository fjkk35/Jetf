using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
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
        /// <summary>
        /// 建立應收未收明細服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">DataCenter 資料庫內容。</param>
        public ReceivableService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
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
        public ReceivableCustomerSelectionOptions GetCustomerSelectionOptions()
        {
            var seaType = CustomerType.SEA.ToString();
            var airType = CustomerType.AIR.ToString();

            var seaCustomers = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == seaType && !string.IsNullOrEmpty(x.CustCode))
                .Select(x => new ReceivableCustomerOption
                {
                    Type = seaType,
                    CustCode = x.CustCode,
                    CustName = x.CustName
                })
                .ToList();

            var airCustomers = DataCenterDb.SysCusts
                .AsNoTracking()
                .Where(x => x.CustType == airType && !string.IsNullOrEmpty(x.OldCode))
                .Select(x => new ReceivableCustomerOption
                {
                    Type = airType,
                    CustCode = x.OldCode,
                    CustName = x.CustName
                })
                .ToList();

            var groups = JetfDb.ReconciliationCustomerGroups
                .AsNoTracking()
                .Include(x => x.Details)
                .OrderBy(x => x.Type)
                .ThenBy(x => x.GroupName)
                .ToList()
                .Select(x => new ReceivableCustomerGroupOption
                {
                    Id = x.Id,
                    Type = x.Type,
                    GroupName = x.GroupName,
                    CustCodes = (x.Details ?? Enumerable.Empty<ReconciliationCustomerGroupDetailEntity>())
                        .Where(detail => !string.IsNullOrWhiteSpace(detail.CustCode))
                        .Select(detail => detail.CustCode.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(code => code)
                        .ToList()
                })
                .ToList();

            return new ReceivableCustomerSelectionOptions
            {
                SeaCustomers = NormalizeCustomerOptions(seaCustomers),
                AirCustomers = NormalizeCustomerOptions(airCustomers),
                Groups = groups
            };
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
                .WhereIf(request?.Status == ReceivableStatus.Received,
                    x => x.ReceivedCustomerCodTime.HasValue || x.ReceivedTransCodTime.HasValue)
                .WhereIf(request?.Status == ReceivableStatus.Unreceived,
                    x => !x.ReceivedCustomerCodTime.HasValue && !x.ReceivedTransCodTime.HasValue)
                .WhereIf(request?.CollectionType == ReceivableCollectionType.Customer,
                    x => (x.CustomerCod ?? 0) > 0)
                .WhereIf(request?.CollectionType == ReceivableCollectionType.Trans,
                    x => (x.TransCod ?? 0) > 0);

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
                ReceivedCustomerCod = x.ReceivedCustomerCod ?? 0,
                ReceivedTransCod = x.ReceivedTransCod ?? 0
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
                var codSubtotal = row.Cod + row.Fee + row.CustomerCod + row.TransCod;
                var receivedAmount = row.ReceivedCustomerCod + row.ReceivedTransCod;
                string customerName;
                customerNames.TryGetValue(row.CustomerCode ?? string.Empty, out customerName);

                return new ReceivableListItem
                {
                    Id = row.Id,
                    PostingDate = row.OutDateTime?.ToString("yyyy/MM/dd"),
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
                    TransCod = row.TransCod,
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
        /// 移除無效或重複的客戶選項並依客戶代號排序。
        /// </summary>
        /// <param name="customers">原始客戶選項。</param>
        /// <returns>正規化後的客戶選項。</returns>
        private static List<ReceivableCustomerOption> NormalizeCustomerOptions(
            IEnumerable<ReceivableCustomerOption> customers)
        {
            return customers
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                .GroupBy(x => x.CustCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new ReceivableCustomerOption
                {
                    Type = group.First().Type,
                    CustCode = group.Key,
                    CustName = group.Select(x => x.CustName)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                })
                .OrderBy(x => x.CustCode)
                .ToList();
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
        /// <returns>完成欄位、資料及格式設定的 Excel 活頁簿。</returns>
        private static IWorkbook CreateExcelWorkbook(List<ReceivableListItem> data)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("應收未收明細");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);
            var headers = new[]
            {
                "序號", "掛帳日", "資料來源", "報關類別", "客戶", "出倉時間",
                "分提單號", "物流貨號", "稅單號碼", "代收小計", "已收金額", "未收金額",
                "跟廠商收", "跟派件收", "捷豐支付", "報關費", "重派運費", "到付款",
                "手續費", "未回收原因"
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
            return workbook;
        }
    }
}
