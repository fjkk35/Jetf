namespace TaxPortalApi.Models.SeaShenzhen;

/// <summary>
/// 新遞金額查詢結果，金額來源比照新遞託運資料查詢頁面。
/// </summary>
public sealed class SeaShenzhenAmountResponse
{
    /// <summary>
    /// 物流貨號。
    /// </summary>
    public string TrackinngNo { get; init; } = string.Empty;

    /// <summary>
    /// 到付款：優先取稅金轉檔的 Cod，沒有轉檔資料則直接取原始 Cc；未填時為 0。
    /// </summary>
    public double Cod { get; init; }

    /// <summary>
    /// 稅金；沒有稅金轉檔資料時為 0。
    /// </summary>
    public int Tax { get; init; }

    /// <summary>
    /// 稅金手續費；沒有稅金轉檔資料時為 0。
    /// </summary>
    public int Fee { get; init; }

    /// <summary>
    /// SeaShenzhenOriginal.TaxPayment 原始值，例如 XD、C；保留 null。
    /// </summary>
    public string? TaxPayment { get; init; }
}
