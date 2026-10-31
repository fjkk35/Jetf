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
            SaveCollectibleRows(feeRows, dataDate);

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

            // 來源一：[dbo].[ShenzhenFeeMasterManualToDlvCod]，取得 ModifiedTime 在所選日期內的人工調整貨號。
            // 僅用貨號篩選後續資料來源，下載欄位及金額優先使用費用主檔，不存在時使用原始託運資料。
            var manualDlvInvs = JetfDb.ShenzhenFeeMasterManualToDlvCods
                .AsNoTracking()
                .Where(x => x.ModifiedTime >= dataDate && x.ModifiedTime < nextDate)
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
                .Select(CreateOriginalDownloadRow);

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
        /// 將原始託運資料轉成下載資料，僅收取其原始代收金額。
        /// </summary>
        private static SeaShenzhenFeeDownloadRow CreateOriginalDownloadRow(SeaShenzhenOriginalEntity original)
        {
            var cod = Convert.ToInt32(Math.Round(original.Cc ?? 0, MidpointRounding.AwayFromZero));
            return new SeaShenzhenFeeDownloadRow
            {
                TrackingNo = original.TrackingNo,
                DlvInv = original.JetfSerial,
                Cod = cod,
                Fee = 0,
                ToDlvCod = cod,
                Recipient = original.Importer,
                RecPhone = original.ImporterPhone,
                DlvCom = original.TransName,
                IncludeTax = original.TaxPayment
            };
        }

        /// <summary>
        /// 將當次下載資料的代收總金額轉入 FEE_MASTER_COD 的 CC 與 ToDlvCod，已銷帳或兩欄金額皆相同的既有資料不更新。
        /// </summary>
        /// <param name="feeRows">與下載檔案相同的來源資料。</param>
        /// <param name="dataDate">下載所選資料日期。</param>
        private void SaveCollectibleRows(List<SeaShenzhenFeeDownloadRow> feeRows, DateTime dataDate)
        {
            // 僅處理當次下載中具有物流貨號的資料。
            var rows = feeRows
                .Where(x => !string.IsNullOrWhiteSpace(x.DlvInv))
                .ToList();
            if (rows.Count == 0)
            {
                return;
            }

            // 查詢及寫入共用交易，防止查詢後才銷帳的資料被覆寫，也保護不存在的單號。
            using (var transaction = JetfDb.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                // 批次查詢既有到付款資料，以物流貨號建立字典，同一貨號保留全部既有資料。
                var dlvInvs = rows.Select(x => x.DlvInv).ToList();
                var existingRowsDic = JetfDb.FeeMasterCods
                    .AsNoTracking()
                    .WhereBulkContains(JetfDb, dlvInvs, x => x.DlvInv, x => x)
                    .GroupBy(x => x.DlvInv.TrimEnd())
                    .ToDictionary(group => group.Key, group => group.ToList());
                var inserts = new List<FeeMasterCodEntity>();
                var updates = new List<FeeMasterCodEntity>();
                var now = DateTime.Now;

                foreach (var row in rows)
                {
                    if (!existingRowsDic.TryGetValue(row.DlvInv.TrimEnd(), out var targets))
                    {
                        // 物流貨號不存在時新增；來源沒有主號，資料日期作為出倉日期。
                        inserts.Add(new FeeMasterCodEntity
                        {
                            DataType = "新遞",
                            MainNumber = string.Empty,
                            Customer = row.Customer,
                            BagNumber = row.TrackingNo,
                            TrackingNo = row.TrackingNo,
                            DlvInv = row.DlvInv,
                            OriginalImporter = row.Recipient,
                            Cc = row.ToDlvCod,
                            ToDlvCod = row.ToDlvCod,
                            SignOutTime = dataDate,
                            CreatedTime = now
                        });
                        continue;
                    }

                    foreach (var target in targets)
                    {
                        // 已有銷帳時間、銷帳紀錄，或 CC 與代收總金額皆相同時，略過該筆更新。
                        if (target.ReceivedCcTime.HasValue || target.ReconciliationLogisticsId.HasValue ||
                            (target.Cc == row.ToDlvCod && target.ToDlvCod == row.ToDlvCod))
                        {
                            continue;
                        }

                        // 到付款及代收總金額皆使用來源 ToDlvCod，保留既有手續費與其他欄位。
                        target.Cc = row.ToDlvCod;
                        target.ToDlvCod = row.ToDlvCod;
                        updates.Add(target);
                    }
                }

                // 批次寫入新增與更新資料，全部成功才提交交易；失敗時由交易回復。
                JetfDb.BulkUpdate(updates);
                JetfDb.BulkInsert(inserts, options =>
                {
                    options.TimeoutSeconds = CommandTimeoutSeconds;
                });
                transaction.Commit();
            }
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
