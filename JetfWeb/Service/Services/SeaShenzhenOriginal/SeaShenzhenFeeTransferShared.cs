using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Service.Services.SeaShenzhenOriginal
{
    /// <summary>
    /// 新遞深圳稅金轉檔共用邏輯。
    /// </summary>
    internal static class SeaShenzhenFeeTransferShared
    {
        private const int FeeWhenTaxPaymentC = 30;

        /// <summary>
        /// 批次查出分號對應的原始託運資料。
        /// </summary>
        public static Dictionary<string, SeaShenzhenOriginalEntity> GetOriginalLookup(JetfDbContext jetfDbContext, IEnumerable<string> trackingNos)
        {
            var normalizedTrackingNos = (trackingNos ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedTrackingNos.Count == 0)
            {
                return new Dictionary<string, SeaShenzhenOriginalEntity>(StringComparer.OrdinalIgnoreCase);
            }

            var originals = jetfDbContext.SeaShenzhenOriginals
                .AsNoTracking()
                .WhereBulkContains(jetfDbContext, normalizedTrackingNos, x => x.TrackingNo, x => x);

            return originals
                .Where(x => !string.IsNullOrWhiteSpace(x.TrackingNo))
                .GroupBy(x => x.TrackingNo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(x => string.IsNullOrWhiteSpace(x.JetfSerial) ? 1 : 0)
                        .ThenBy(x => GetJetfSerialNumber(x.JetfSerial))
                        .ThenBy(x => x.JetfSerial, StringComparer.OrdinalIgnoreCase)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依原始託運資料與稅金建立深圳稅金轉檔資料列。
        /// </summary>
        public static ShenzhenFeeMasterEntity CreateTransferRow(
            SeaShenzhenOriginalEntity original,
            int tax,
            string dataDate,
            string customer,
            SeaShenzhenTaxDataType dataType,
            int? feeMasterId,
            string userId,
            DateTime now)
        {
            var cod = ToAmount(original.Cc);
            var includeTax = tax >= 1000
                ? ShenzhenTaxPayment.C.ToString()
                : original.TaxPayment;
            var fee = string.Equals(includeTax, ShenzhenTaxPayment.C.ToString(), StringComparison.OrdinalIgnoreCase)
                ? FeeWhenTaxPaymentC
                : 0;

            return new ShenzhenFeeMasterEntity
            {
                FeeMasterId = feeMasterId,
                DataDate = dataDate,
                DataType = dataType,
                Customer = customer,
                TrackingNo = original.TrackingNo,
                DlvInv = original.JetfSerial,
                Tax = tax,
                Cod = cod,
                Fee = fee,
                IncludeTax = includeTax,
                DlvCom = original.TransName,
                Recipient = original.Importer,
                RecPhone = original.ImporterPhone,
                RecAddress = original.ImporterAddress,
                ToDlvCod = tax + cod + fee,
                CreatedUser = userId,
                CreatedTime = now,
                ModifiedUser = userId,
                ModifiedTime = now
            };
        }

        /// <summary>
        /// 將原始代收金額轉成轉檔表使用的整數金額。
        /// </summary>
        private static int ToAmount(double? value)
        {
            if (!value.HasValue)
            {
                return 0;
            }

            return Convert.ToInt32(Math.Round(value.Value, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// 取得物流貨號排序用數值，非純數字貨號排在純數字貨號之後。
        /// </summary>
        private static decimal GetJetfSerialNumber(string jetfSerial)
        {
            decimal number;
            return decimal.TryParse((jetfSerial ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out number)
                ? number
                : decimal.MaxValue;
        }
    }
}
