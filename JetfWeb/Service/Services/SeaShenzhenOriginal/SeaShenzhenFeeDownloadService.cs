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
    /// 新遞深圳物流代收檔下載服務。
    /// </summary>
    public class SeaShenzhenFeeDownloadService : _BaseService
    {
        public SeaShenzhenFeeDownloadService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依資料日期匯出 IncludeTax = C 的深圳物流代收檔。
        /// </summary>
        public byte[] ExportCollectibleExcel(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = GetRequiredDataDate(request);

            var feeRows = JetfDb.ShenzhenFeeMasters
                .AsNoTracking()
                .Where(x => x.DataDate == dataDate && x.IncludeTax == "C")
                .OrderBy(x => x.Id)
                .ToList();

            if (feeRows.Count == 0)
            {
                throw new Exception("查無符合條件的物流代收資料");
            }

            var customerNameLookup = GetCustomerNameLookup(feeRows);
            var workbook = CreateCollectibleWorkbook(feeRows, customerNameLookup);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

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

        private IWorkbook CreateCollectibleWorkbook(
            IEnumerable<ShenzhenFeeMasterEntity> feeRows,
            Dictionary<string, string> customerNameLookup)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("物流代收檔");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var headers = new[]
            {
                "項次",
                "客戶",
                "清關袋號",
                "運單號",
                "稅金",
                "納稅義務人",
                "電話",
                "備註",
                "派件公司",
                "稅金類別"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            var rowIndex = 1;
            var serialNo = 1;
            foreach (var item in feeRows)
            {
                var row = sheet.CreateRow(rowIndex++);
                var customerName = GetCustomerName(item.Customer, customerNameLookup);

                NpoiCell.CreateIntCell(row, 0, serialNo++, dataStyle);
                NpoiCell.CreateCell(row, 1, customerName, dataStyle);
                NpoiCell.CreateCell(row, 2, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, 3, item.DlvInv, dataStyle);
                NpoiCell.CreateIntCell(row, 4, item.ToDlvCod, dataStyle);
                NpoiCell.CreateCell(row, 5, item.Recipient, dataStyle);
                NpoiCell.CreateCell(row, 6, item.RecPhone, dataStyle);
                NpoiCell.CreateCell(row, 7, string.Empty, dataStyle);
                NpoiCell.CreateCell(row, 8, item.DlvCom, dataStyle);
                NpoiCell.CreateCell(row, 9, GetTaxPaymentDescription(item.IncludeTax), dataStyle);
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

        private static string GetRequiredDataDate(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = (request?.DataDate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dataDate))
            {
                throw new Exception("請輸入資料日期");
            }

            return dataDate;
        }

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

        private static string GetTaxPaymentDescription(string includeTax)
        {
            var taxPayment = ShenzhenTaxPaymentExtensions.ParseNullableCode(includeTax);
            return taxPayment.HasValue ? taxPayment.Value.ToDescription() : string.Empty;
        }
    }
}
