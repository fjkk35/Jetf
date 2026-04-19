namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 費用資料傳輸物件。
/// </summary>
public sealed class FeeMasterDto
{
    /// <summary>
    /// 單號。
    /// </summary>
    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 稅金。
    /// </summary>
    public int Tax { get; set; }

    /// <summary>
    /// 報關費。
    /// </summary>
    public int Ccfee { get; set; }

    /// <summary>
    /// 到付款。
    /// </summary>
    public int Cod { get; set; }

    /// <summary>
    /// 手續費
    /// </summary>
    public int Fee { get; set; }
}
