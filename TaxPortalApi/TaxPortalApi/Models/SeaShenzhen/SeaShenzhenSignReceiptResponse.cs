namespace TaxPortalApi.Models.SeaShenzhen;

/// <summary>
/// 新遞簽收單查詢結果。
/// </summary>
public sealed class SeaShenzhenSignReceiptResponse
{
    /// <summary>
    /// 物流貨號。
    /// </summary>
    public string TrackinngNo { get; init; } = string.Empty;

    /// <summary>
    /// 簽收圖片清單，同一貨號可能有多張圖片。
    /// </summary>
    public IReadOnlyList<SeaShenzhenSignReceiptImage> Images { get; init; } = [];
}
