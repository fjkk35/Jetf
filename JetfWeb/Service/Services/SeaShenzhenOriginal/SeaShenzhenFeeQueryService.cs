using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞稅金資料查詢服務。
    /// </summary>
    public class SeaShenzhenFeeQueryService : _BaseService
    {
        public SeaShenzhenFeeQueryService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依條件查詢新遞稅金資料。
        /// </summary>
        public SeaShenzhenFeeQueryResponse GetData(SeaShenzhenFeeQueryRequest request)
        {
            request = request ?? new SeaShenzhenFeeQueryRequest();

            var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;
            pageSize = Math.Min(pageSize, 200);

            var query = BuildQuery(request);
            var totalCount = query.Count();

            var feeRows = query
                .OrderByDescending(x => x.DataDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new SeaShenzhenFeeQueryResponse
            {
                TotalCount = totalCount,
                Data = BuildQueryRows(feeRows)
            };
        }

        /// <summary>
        /// 匯出新遞稅金資料查詢結果。
        /// </summary>
        public byte[] ExportExcel(SeaShenzhenFeeQueryRequest request)
        {
            request = request ?? new SeaShenzhenFeeQueryRequest();

            var feeRows = BuildQuery(request)
                .OrderByDescending(x => x.DataDate)
                .ThenByDescending(x => x.Id)
                .ToList();

            if (feeRows.Count == 0)
            {
                throw new Exception("查無符合條件的稅金資料");
            }

            var rows = BuildQueryRows(feeRows);
            var workbook = CreateWorkbook(rows);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 依查詢條件建立稅金資料查詢 IQueryable。
        /// </summary>
        private IQueryable<ShenzhenFeeMasterEntity> BuildQuery(SeaShenzhenFeeQueryRequest request)
        {
            var dataDateStart = NormalizeDataDate(request.DataDateStart);
            var dataDateEnd = NormalizeDataDate(request.DataDateEnd);
            var trackingNo = NullIfEmpty(request.TrackingNo);
            var dlvInv = NullIfEmpty(request.DlvInv);
            var includeTax = NullIfEmpty(request.IncludeTax);
            var dataType = NullIfEmpty(request.DataType);

            var query = JetfDb.ShenzhenFeeMasters.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(dataDateStart))
            {
                query = query.Where(x => string.Compare(x.DataDate, dataDateStart) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(dataDateEnd))
            {
                query = query.Where(x => string.Compare(x.DataDate, dataDateEnd) <= 0);
            }

            if (!string.IsNullOrWhiteSpace(trackingNo))
            {
                query = query.Where(x => x.TrackingNo.Contains(trackingNo));
            }

            if (!string.IsNullOrWhiteSpace(dlvInv))
            {
                query = query.Where(x => x.DlvInv.Contains(dlvInv));
            }

            if (!string.IsNullOrWhiteSpace(dataType))
            {
                SeaShenzhenTaxDataType dataTypeValue;
                if (!Enum.TryParse(dataType, true, out dataTypeValue))
                {
                    throw new Exception("報關行格式錯誤");
                }

                query = query.Where(x => x.DataType == dataTypeValue);
            }

            if (!string.IsNullOrWhiteSpace(includeTax))
            {
                query = query.Where(x => x.IncludeTax == includeTax);
            }

            return query;
        }

        /// <summary>
        /// 將查詢到的資料轉成畫面與 Excel 共用的列資料。
        /// </summary>
        private List<SeaShenzhenFeeQueryRow> BuildQueryRows(IList<ShenzhenFeeMasterEntity> feeRows)
        {
            var customerNameLookup = GetCustomerNameLookup(feeRows);

            return (feeRows ?? Array.Empty<ShenzhenFeeMasterEntity>())
                .Select(x => new SeaShenzhenFeeQueryRow
                {
                    Id = x.Id,
                    DataDateText = FormatDataDate(x.DataDate),
                    CustomerName = GetCustomerName(x.Customer, customerNameLookup),
                    DataTypeDisplay = GetDataTypeDescription(x.DataType),
                    DlvCom = x.DlvCom,
                    TrackingNo = x.TrackingNo,
                    DlvInv = x.DlvInv,
                    IncludeTaxDisplay = GetTaxPaymentDisplay(x.IncludeTax),
                    Tax = x.Tax,
                    Cod = x.Cod,
                    Fee = x.Fee,
                    ToDlvCod = x.ToDlvCod
                })
                .ToList();
        }

        /// <summary>
        /// 依查詢結果補齊客戶代碼對應的客戶名稱。
        /// </summary>
        private Dictionary<string, string> GetCustomerNameLookup(IEnumerable<ShenzhenFeeMasterEntity> feeRows)
        {
            var customerCodes = (feeRows ?? Enumerable.Empty<ShenzhenFeeMasterEntity>())
                .Select(x => x.Customer)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (customerCodes.Count == 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var customers = DataCenterDb.SysCusts
                .AsNoTracking()
                .WhereBulkContains(DataCenterDb, customerCodes, x => x.CustCode, x => x);

            return customers
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                .GroupBy(x => x.CustCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.CustName).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 將空白字串正規化為 null。
        /// </summary>
        private static string NullIfEmpty(string value)
        {
            var trimmedValue = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(trimmedValue) ? null : trimmedValue;
        }

        /// <summary>
        /// 將輸入日期文字正規化為 yyyyMMdd。
        /// </summary>
        private static string NormalizeDataDate(string value)
        {
            var trimmedValue = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                return null;
            }

            DateTime dateValue;
            if (DateTime.TryParse(trimmedValue, out dateValue))
            {
                return dateValue.ToString("yyyyMMdd");
            }

            trimmedValue = trimmedValue.Replace("-", string.Empty).Replace("/", string.Empty);
            if (trimmedValue.Length == 8 && trimmedValue.All(char.IsDigit))
            {
                return trimmedValue;
            }

            throw new Exception("日期格式錯誤");
        }

        /// <summary>
        /// 將 yyyyMMdd 顯示為 yyyy-MM-dd。
        /// </summary>
        private static string FormatDataDate(string value)
        {
            var normalizedValue = NormalizeDataDate(value);
            DateTime dateValue;
            return DateTime.TryParseExact(normalizedValue, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out dateValue)
                ? dateValue.ToString("yyyy-MM-dd")
                : normalizedValue;
        }

        /// <summary>
        /// 取得顯示用客戶名稱，查無主檔時退回原始代碼。
        /// </summary>
        private static string GetCustomerName(string customerCode, Dictionary<string, string> customerNameLookup)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
            {
                return string.Empty;
            }

            string customerName;
            if (customerNameLookup != null && customerNameLookup.TryGetValue(customerCode, out customerName) && !string.IsNullOrWhiteSpace(customerName))
            {
                return customerName;
            }

            return customerCode;
        }

        /// <summary>
        /// 將稅金支付方式代碼轉成顯示用文字。
        /// </summary>
        private static string GetTaxPaymentDisplay(string includeTax)
        {
            var taxPayment = EnumerableExtensions.ParseNullableCode<ShenzhenTaxPayment>(includeTax);
            return taxPayment.HasValue ? taxPayment.Value.ToDescription() : string.Empty;
        }

        /// <summary>
        /// 將報關行代碼轉成顯示用文字。
        /// </summary>
        private static string GetDataTypeDescription(SeaShenzhenTaxDataType dataType)
        {
            return dataType.ToDescription();
        }

        /// <summary>
        /// 建立稅金資料查詢的 Excel 檔案。
        /// </summary>
        private static IWorkbook CreateWorkbook(IEnumerable<SeaShenzhenFeeQueryRow> rows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("稅金資料查詢");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var headers = new[]
            {
                "序號",
                "日期",
                "客戶",
                "報關行",
                "派件公司",
                "分提單號",
                "物流貨號",
                "稅金支付方式",
                "稅金",
                "到付款",
                "手續費",
                "物流代收金額"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            var rowIndex = 1;
            var serialNo = 1;
            foreach (var item in rows ?? Enumerable.Empty<SeaShenzhenFeeQueryRow>())
            {
                var row = sheet.CreateRow(rowIndex++);
                NpoiCell.CreateIntCell(row, 0, serialNo++, dataStyle);
                NpoiCell.CreateCell(row, 1, item.DataDateText, dataStyle);
                NpoiCell.CreateCell(row, 2, item.CustomerName, dataStyle);
                NpoiCell.CreateCell(row, 3, item.DataTypeDisplay, dataStyle);
                NpoiCell.CreateCell(row, 4, item.DlvCom, dataStyle);
                NpoiCell.CreateCell(row, 5, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, 6, item.DlvInv, dataStyle);
                NpoiCell.CreateCell(row, 7, item.IncludeTaxDisplay, dataStyle);
                NpoiCell.CreateIntCell(row, 8, item.Tax, dataStyle);
                NpoiCell.CreateIntCell(row, 9, item.Cod, dataStyle);
                NpoiCell.CreateIntCell(row, 10, item.Fee, dataStyle);
                NpoiCell.CreateIntCell(row, 11, item.ToDlvCod, dataStyle);
            }

            for (var index = 0; index < headers.Length; index++)
            {
                sheet.AutoSizeColumn(index);
                if (sheet.GetColumnWidth(index) < 3000)
                {
                    sheet.SetColumnWidth(index, 3000);
                }
            }

            return workbook;
        }
    }
}
