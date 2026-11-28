using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞深圳物流代收檔下載服務。
    /// </summary>
    public class SeaShenzhenFeeDownloadService : _BaseService
    {
        private const int CommandTimeoutSeconds = 1200;

        public SeaShenzhenFeeDownloadService(JetfDbContext jetfDbContext, DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
            JetfDb.Database.CommandTimeout = CommandTimeoutSeconds;
        }

        /// <summary>
        /// 依日期先轉入到付款資料，再產生深圳費用、原始代收與當日人工調整的物流代收檔。
        /// 已銷帳資料仍可下載，但不更新到付款金額。
        /// </summary>
        public byte[] ExportCollectibleExcel(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = GetRequiredDataDate(request);

            var feeRows = GetCollectibleRows(dataDate);

            if (feeRows.Count == 0)
            {
                throw new Exception("查無符合條件的物流代收資料");
            }

            // 先將當次下載資料轉入到付款資料，資料庫更新完成後才產生檔案；銷帳狀態僅影響寫入。
            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                SeaShenzhenCodTransfer.SaveCollectibleRows(JetfDb, feeRows, dataDate);
                transaction.Commit();
            }

            var customerNameDic = GetCustomerNameDic(feeRows);
            var workbook = CreateCollectibleWorkbook(feeRows, customerNameDic);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 依日期取得物流代收資料，並以物流貨號排除補入來源的重複資料。
        /// </summary>
        private List<SeaShenzhenFeeDownloadRow> GetCollectibleRows(DateTime dataDate)
        {
            var dataDateText = dataDate.ToString("yyyyMMdd");
            var nextDate = dataDate.AddDays(1);
            var taxPaymentC = ShenzhenTaxPayment.C.ToString();

            // 來源一：[dbo].[ShenzhenFeeMasterManualToDlvCod]，依調整紀錄的建立時間取得當日貨號。
            // 僅用貨號篩選後續資料來源，下載欄位及金額優先使用費用主檔，不存在時使用原始託運資料。
            var manualDlvInvs = JetfDb.ShenzhenFeeMasterManualToDlvCods
                .AsNoTracking()
                .Where(x => x.CreatedTime >= dataDate && x.CreatedTime < nextDate)
                .Select(x => x.DlvInv)
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            // 來源二：[dbo].[ShenzhenFeeMaster]，取得所選 DataDate 且符合 IncludeTax = C 或 Cod > 0 的既有下載資料。
            // 同時取得人工調整貨號的歷史費用資料，使用主檔目前的下載欄位與金額。
            var feeMasters = JetfDb.ShenzhenFeeMasters
                .AsNoTracking()
                .Where(x => (x.DataDate == dataDateText && (x.IncludeTax == taxPaymentC || x.Cod > 0)) ||
                    manualDlvInvs.Contains(x.DlvInv))
                .OrderBy(x => x.Id)
                .ToList();

            // 來源三：[dbo].[SeaShenzhenOriginal]，取得所選 DataDate 且 Cc > 0 的原始託運資料。
            // 同時取得人工調整貨號的歷史託運資料，費用主檔沒有該貨號時，改用原始託運資料。
            var originals = JetfDb.SeaShenzhenOriginals
                .AsNoTracking()
                .Where(x => (x.DataDate >= dataDate && x.DataDate < nextDate && x.Cc > 0) ||
                    manualDlvInvs.Contains(x.JetfSerial))
                .OrderBy(x => x.Id)
                .ToList();

            // 費用與原始託運資料在記憶體中合併，僅補入物流貨號尚未存在的資料。
            return MergeCollectibleRows(feeMasters, originals);
        }

        /// <summary>
        /// 在記憶體中合併下載資料，優先使用費用主檔，貨號不存在時才使用原始託運資料。
        /// </summary>
        private static List<SeaShenzhenFeeDownloadRow> MergeCollectibleRows(
            List<ShenzhenFeeMasterEntity> feeMasters,
            List<SeaShenzhenOriginalEntity> originals)
        {
            // 以費用主檔的 DlvInv 比對原始託運資料的 JetfSerial，僅保留主檔中不存在的貨號，並轉成下載資料。
            var originalRows = originals
                .Where(original => !feeMasters.Any(fee => fee.DlvInv == original.JetfSerial))
                .Select(original => SeaShenzhenCodTransfer.CreateOriginalRow(
                    original, SeaShenzhenFeeTransferShared.GetOriginalCod(original.Cc)));

            // 優先保留費用主檔的欄位與金額，再接上原始託運資料補入的資料，產生下載清單。
            return feeMasters
                .Select(CreateFeeDownloadRow)
                .Concat(originalRows)
                .ToList();
        }

        /// <summary>
        /// 將深圳費用主檔轉成下載資料。
        /// </summary>
        private static SeaShenzhenFeeDownloadRow CreateFeeDownloadRow(ShenzhenFeeMasterEntity fee)
        {
            return new SeaShenzhenFeeDownloadRow
            {
                Id = fee.Id,
                Customer = fee.Customer,
                TrackingNo = fee.TrackingNo,
                DlvInv = fee.DlvInv,
                Cod = fee.Cod,
                Fee = fee.Fee,
                ToDlvCod = fee.ToDlvCod,
                Recipient = fee.Recipient,
                RecPhone = fee.RecPhone,
                DlvCom = fee.DlvCom,
                IncludeTax = fee.IncludeTax
            };
        }

        /// <summary>
        /// 依匯出資料補齊客戶代碼對應的客戶名稱。
        /// </summary>
        private Dictionary<string, string> GetCustomerNameDic(IEnumerable<SeaShenzhenFeeDownloadRow> feeRows)
        {
            var customerCodes = (feeRows ?? Enumerable.Empty<SeaShenzhenFeeDownloadRow>())
                .Select(x => x.Customer)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (customerCodes.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            var customers = DataCenterDb.SysCusts
                .AsNoTracking()
                .WhereBulkContains(DataCenterDb, customerCodes, x => x.CustCode, x => x);

            return customers
                .Where(x => !string.IsNullOrWhiteSpace(x.CustCode))
                .GroupBy(x => x.CustCode)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.CustName).FirstOrDefault() ?? string.Empty);
        }

        /// <summary>
        /// 建立物流代收檔 Excel。
        /// </summary>
        private IWorkbook CreateCollectibleWorkbook(
            IEnumerable<SeaShenzhenFeeDownloadRow> feeRows,
            Dictionary<string, string> customerNameDic)
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
                var customerName = GetCustomerName(item.Customer, customerNameDic);

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
        private static DateTime GetRequiredDataDate(SeaShenzhenFeeTransferRequest request)
        {
            var dataDate = (request?.DataDate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dataDate))
            {
                throw new Exception("請輸入資料日期");
            }

            DateTime parsedDate;
            if (!DateTime.TryParseExact(dataDate, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsedDate))
            {
                throw new Exception("資料日期格式錯誤");
            }

            return parsedDate;
        }

        /// <summary>
        /// 取得顯示用客戶名稱，查無主檔時退回原始代碼。
        /// </summary>
        private static string GetCustomerName(string customerCode, Dictionary<string, string> customerNameDic)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
            {
                return string.Empty;
            }

            string customerName;
            if (customerNameDic != null && customerNameDic.TryGetValue(customerCode, out customerName) && !string.IsNullOrWhiteSpace(customerName))
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
