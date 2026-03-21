using System.ComponentModel.DataAnnotations;

namespace TaxPortalApi.Models.TaxDocuments;

/// <summary>
/// 稅金單查詢請求資料。
/// </summary>
public sealed class TaxDocumentQueryRequest
{
    /// <summary>
    /// 稅單號碼。
    /// </summary>
    [Required(ErrorMessage = "請輸入稅單號碼")]
    public string TaxNumber { get; init; } = string.Empty;
}