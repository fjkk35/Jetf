namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 異常原因資料傳輸物件。
/// </summary>
public sealed class ShipmentInboundExceptionReasonDto
{
    /// <summary>
    /// 異常原因 Id。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 異常原因。
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
