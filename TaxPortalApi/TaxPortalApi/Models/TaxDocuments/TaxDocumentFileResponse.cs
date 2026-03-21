namespace TaxPortalApi.Models.TaxDocuments;

/// <summary>
/// 稅金單檔案 API 回應資料。
/// </summary>
public sealed class TaxDocumentFileResponse
{
    /// <summary>
    /// 下載檔名。
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 檔案內容類型。
    /// </summary>
    public string ContentType { get; init; } = "application/pdf";

    /// <summary>
    /// Base64 編碼後的 PDF 內容。
    /// </summary>
    public string ContentBase64 { get; init; } = string.Empty;
}