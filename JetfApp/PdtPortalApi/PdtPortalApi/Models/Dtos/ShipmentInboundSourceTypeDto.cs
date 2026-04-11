namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 貨件來源資料傳輸物件。
/// </summary>
public sealed class ShipmentInboundSourceTypeDto
{
    /// <summary>
    /// 貨件來源識別碼。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 貨件來源名稱。
    /// </summary>
    public string SourceType { get; set; } = string.Empty;
}