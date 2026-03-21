namespace TaxPortalApi.Models.TaxDocuments;

/// <summary>
/// 稅金單檔案下載結果。
/// </summary>
public sealed class TaxDocumentFileResult
{
    /// <summary>
    /// PDF 檔案內容。
    /// </summary>
    public byte[] Content { get; init; } = [];

    /// <summary>
    /// 回應的內容類型。
    /// </summary>
    public string ContentType { get; init; } = "application/pdf";

    /// <summary>
    /// 下載檔名。
    /// </summary>
    public string FileName { get; init; } = string.Empty;
}