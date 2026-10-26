namespace TaxPortalApi.Models.SeaShenzhen;

/// <summary>
/// 一張簽收圖片。
/// </summary>
public sealed class SeaShenzhenSignReceiptImage
{
    /// <summary>
    /// 圖片檔名。
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 圖片 MIME 類型，例如 image/jpeg。
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// 圖片的 Base64 內容，不含 data URI 前綴。
    /// </summary>
    public string ContentBase64 { get; init; } = string.Empty;
}
