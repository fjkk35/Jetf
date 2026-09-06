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
        /// <summary>
        /// 物流代收檔匯出用列資料。
        /// </summary>
        private sealed class SeaShenzhenFeeDownloadRow
        {
            public string Customer { get; set; }

            public string TrackingNo { get; set; }

            public string DlvInv { get; set; }

            public int ToDlvCod { get; set; }

            public string Recipient { get; set; }

            public string RecPhone { get; set; }

            public string DlvCom { get; set; }

            public string IncludeTax { get; set; }
        }

        public SeaShenzhenFeeDownloadService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 依資料日期匯出 IncludeTax = C 或 Cod > 0 的深圳物流代收檔。
        /// </summary>
        public byte[] ExportCollectibleExcel(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = GetRequiredDataDate(request);

            var feeRows = GetCollectibleRows(dataDate);

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

        /// <summary>
        /// 依資料日期整理可下載的物流代收資料，符合 IncludeTax = C 或 Cod > 0 的資料才可下載。
        /// </summary>
        private List<SeaShenzhenFeeDownloadRow> GetCollectibleRows(string dataDate)
        {
            var collectibleTaxPayment = ShenzhenTaxPayment.C.ToString();

            var dateFeeRows = JetfDb.ShenzhenFeeMasters
                .AsNoTracking()
                .Where(x => x.DataDate == dataDate)
                .OrderBy(x => x.Id)
                .ToList();

            if (dateFeeRows.Count == 0)
            {
                return new List<SeaShenzhenFeeDownloadRow>();
            }

            var trackingNos = dateFeeRows
                .Select(x => x.TrackingNo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var manualByTrackingNo = GetManualToDlvCodRows(trackingNos)
                .ToDictionary(x => x.TrackingNo, x => x, StringComparer.OrdinalIgnoreCase);

            var result = new List<SeaShenzhenFeeDownloadRow>();
            foreach (var feeRow in dateFeeRows)
            {
                ShenzhenFeeMasterManualToDlvCodEntity manualRow;
                manualByTrackingNo.TryGetValue(feeRow.TrackingNo, out manualRow);

                // 下載條件：IncludeTax = C 或 Cod > 0。
                if (feeRow.IncludeTax != collectibleTaxPayment && feeRow.Cod <= 0)
                {
                    continue;
                }

                result.Add(new SeaShenzhenFeeDownloadRow
                {
                    Customer = feeRow.Customer,
                    TrackingNo = feeRow.TrackingNo,
                    DlvInv = feeRow.DlvInv,
                    ToDlvCod = manualRow != null ? manualRow.ToDlvCod : feeRow.ToDlvCod,
                    Recipient = feeRow.Recipient,
                    RecPhone = feeRow.RecPhone,
                    DlvCom = feeRow.DlvCom,
                    IncludeTax = manualRow != null ? collectibleTaxPayment : feeRow.IncludeTax
                });
            }

            return result;
        }

        /// <summary>
        /// 依 TrackingNo 批次載入人工代收金額調整資料。
        /// 使用 WhereBulkContains 將 TrackingNo 一次寫入暫存表比對，避免大量 Contains 產生過長 IN 條件或參數限制。
        /// </summary>
        private List<ShenzhenFeeMasterManualToDlvCodEntity> GetManualToDlvCodRows(List<string> trackingNos)
        {
            if (trackingNos == null || trackingNos.Count == 0)
            {
                return new List<ShenzhenFeeMasterManualToDlvCodEntity>();
            }

            return JetfDb.ShenzhenFeeMasterManualToDlvCods
                .AsNoTracking()
                .WhereBulkContains(JetfDb, trackingNos, row => row.TrackingNo, key => key)
                .OrderBy(x => x.Id)
                .ToList();
        }

        /// <summary>
        /// 依匯出資料補齊客戶代碼對應的客戶名稱。
        /// </summary>
        private Dictionary<string, string> GetCustomerNameLookup(IEnumerable<SeaShenzhenFeeDownloadRow> feeRows)
        {
            var customerCodes = (feeRows ?? Enumerable.Empty<SeaShenzhenFeeDownloadRow>())
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
        /// 建立物流代收檔 Excel。
        /// </summary>
        private IWorkbook CreateCollectibleWorkbook(
            IEnumerable<SeaShenzhenFeeDownloadRow> feeRows,
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

        /// <summary>
        /// 驗證並取得必要的資料日期參數。
        /// </summary>
        private static string GetRequiredDataDate(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = (request?.DataDate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dataDate))
            {
                throw new Exception("請輸入資料日期");
            }

            return dataDate;
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
        /// 將稅金支付方式代碼轉成顯示用中文。
        /// </summary>
        private static string GetTaxPaymentDescription(string includeTax)
        {
            var taxPayment = EnumerableExtensions.ParseNullableCode<ShenzhenTaxPayment>(includeTax);
            return taxPayment.HasValue ? taxPayment.Value.ToDescription() : string.Empty;
        }
    }
}
