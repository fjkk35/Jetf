namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 入庫用的製單或原單進口人/收件人資料。
/// </summary>
public sealed class ShipmentImporterDataDto
{
    /// <summary>
    /// 進口人證號或收件人證號。
    /// </summary>
    public string ImporterId { get; set; } = string.Empty;

    /// <summary>
    /// 進口人或收件人名稱。
    /// </summary>
    public string Importer { get; set; } = string.Empty;
}
