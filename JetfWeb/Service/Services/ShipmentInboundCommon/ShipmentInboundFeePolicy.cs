using Service.EnumTax;
using System;
using System.Collections.Generic;

namespace Service.Services.ShipmentInboundCommon
{
    /// <summary>
    /// 集中管理貨件入庫的手續費規則。
    /// </summary>
    public static class ShipmentInboundFeePolicy
    {
        /// <summary>
        /// 特殊手續費規則的客戶代號。
        /// </summary>
        private static readonly HashSet<string> SpecialFeeCustomerCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "00043", // ECMS(日本)
                "00041"  // ESMS(JP)
            };

        /// <summary>
        /// 判斷指定客戶是否套用特殊手續費規則。
        /// </summary>
        /// <param name="custCode">客戶代號。</param>
        /// <returns>需要套用特殊手續費規則時回傳 true，否則回傳 false。</returns>
        public static bool IsSpecialFeeCustomer(string custCode)
        {
            return !string.IsNullOrWhiteSpace(custCode)
                && SpecialFeeCustomerCodes.Contains(custCode.Trim());
        }

        /// <summary>
        /// 依入庫匯入金額資料計算手續費。
        /// </summary>
        /// <param name="custCode">客戶代號。</param>
        /// <param name="tax">稅金。</param>
        /// <param name="ccFee">報關費。</param>
        /// <param name="freightFee">重出運費。</param>
        /// <returns>手續費。</returns>
        public static int CalculateInboundFee(string custCode, int? tax, int? ccFee, int? freightFee)
        {
            if (IsSpecialFeeCustomer(custCode))
            {
                return 0;
            }

            return HasPositiveAmount(tax)
                || HasPositiveAmount(ccFee)
                || HasPositiveAmount(freightFee)
                ? 30
                : 0;
        }

        /// <summary>
        /// 依回倉處理資料計算手續費。
        /// </summary>
        /// <param name="custCode">客戶代號。</param>
        /// <param name="processType">處理方式。</param>
        /// <param name="processTransNo">重出派件公司代碼。</param>
        /// <param name="freightPayerNo">重出運費支付方代碼。</param>
        /// <param name="freightFee">重出運費。</param>
        /// <param name="tax">稅金。</param>
        /// <param name="ccFee">報關費。</param>
        /// <returns>手續費。</returns>
        public static int CalculateProcessFee(
            string custCode,
            ShipmentInboundProcessType? processType,
            byte? processTransNo,
            byte? freightPayerNo,
            int? freightFee,
            int? tax,
            int? ccFee)
        {
            if (processType == ShipmentInboundProcessType.SelfPickup)
            {
                return 0;
            }

            if (IsSpecialFeeCustomer(custCode))
            {
                // 00043 / 00041 只依重出運費判斷手續費
                return HasPositiveAmount(freightFee) ? 30 : 0;
            }

            // 僅在「開新單號重出 + 7-11」時，才額外依運費支付方判斷手續費。
            if (processType == ShipmentInboundProcessType.NewTrackingNo
                && processTransNo == (byte)ShipmentInboundProcessTransNo.SevenEleven)
            {
                return freightPayerNo == (byte)ShipmentInboundFreightPayerNo.Consignee ? 30 : 0;
            }

            // 其餘客戶維持原規則：重出運費、稅金、報關費任一金額大於 0 即收手續費。
            return HasPositiveAmount(freightFee)
                || HasPositiveAmount(tax)
                || HasPositiveAmount(ccFee)
                ? 30
                : 0;
        }

        /// <summary>
        /// 對非 null 手續費套用入庫匯入客戶歸零規則。
        /// </summary>
        /// <param name="custCode">客戶代號。</param>
        /// <param name="fee">原始手續費。</param>
        /// <returns>正規化後的手續費。</returns>
        public static int NormalizeFee(string custCode, int fee)
        {
            return IsSpecialFeeCustomer(custCode) ? 0 : fee;
        }

        /// <summary>
        /// 對可為 null 的手續費套用入庫匯入客戶歸零規則。
        /// </summary>
        /// <param name="custCode">客戶代號。</param>
        /// <param name="fee">原始手續費。</param>
        /// <returns>正規化後的手續費。</returns>
        public static int? NormalizeFee(string custCode, int? fee)
        {
            return IsSpecialFeeCustomer(custCode) ? 0 : fee;
        }

        private static bool HasPositiveAmount(int? amount)
        {
            return (amount ?? 0) > 0;
        }
    }
}
