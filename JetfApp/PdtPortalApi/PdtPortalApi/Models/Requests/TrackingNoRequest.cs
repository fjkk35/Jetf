using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 單號查詢請求。
/// </summary>
public sealed class TrackingNoRequest
{
    /// <summary>
    /// 單號。
    /// </summary>
    [Required(ErrorMessage = "TrackingNo 為必填")]
    public string TrackingNo { get; set; } = string.Empty;
}