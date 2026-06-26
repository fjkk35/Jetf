using System.Collections.Generic;

namespace PdtPortalApi.Services;

/// <summary>
/// 集中管理貨件入庫的手續費規則。
/// </summary>
public static class ShipmentInboundFeePolicy
{
    /// <summary>
    /// 特殊手續費規則的客戶代號。
    /// </summary>
    private static readonly HashSet<string> SpecialFeeCustomerCodes =
        new(StringComparer.OrdinalIgnoreCase)
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
    /// 依入庫金額資料計算手續費。
    /// </summary>
    /// <param name="custCode">客戶代號。</param>
    /// <param name="tax">稅金。</param>
    /// <param name="ccFee">報關費。</param>
    /// <returns>手續費。</returns>
    public static int CalculateInboundFee(string custCode, int? tax, int? ccFee)
    {
        if (IsSpecialFeeCustomer(custCode))
        {
            return 0;
        }

        return HasPositiveAmount(tax)
            || HasPositiveAmount(ccFee)
            ? 30
            : 0;
    }

    private static bool HasPositiveAmount(int? amount)
    {
        return (amount ?? 0) > 0;
    }
}