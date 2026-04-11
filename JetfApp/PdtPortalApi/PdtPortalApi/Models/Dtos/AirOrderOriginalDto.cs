namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 空運原單資料傳輸物件。
/// </summary>
public sealed class AirOrderOriginalDto
{
    /// <summary>
    /// 單號。
    /// </summary>
    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 進口人姓名或收件人名稱。
    /// </summary>
    public string Importer { get; set; } = string.Empty;

    /// <summary>
    /// 進口人或收件人電話。
    /// </summary>
    public string ImporterPhone { get; set; } = string.Empty;

    /// <summary>
    /// 進口人或收件人地址。
    /// </summary>
    public string ImporterAddr { get; set; } = string.Empty;

    /// <summary>
    /// 客戶代碼（CustCode）。
    /// </summary>
    public string CustCode { get; set; } = string.Empty;

    /// <summary>
    /// 承運商代號（TransNo）。
    /// </summary>
    public string TransNo { get; set; } = string.Empty;
}