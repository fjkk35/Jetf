using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 異常件建立請求。
/// </summary>
public sealed class CreateShipmentInboundExceptionRequest
{
    /// <summary>
    /// 流水號。
    /// </summary>
    [Required(ErrorMessage = "SeqNo 為必填")]
    public string SeqNo { get; set; } = string.Empty;

    /// <summary>
    /// 異常原因。
    /// </summary>
    [Required(ErrorMessage = "Reason 為必填")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 照片（Base64）。
    /// </summary>
    [Required(ErrorMessage = "Photo 為必填")]
    public string Photo { get; set; } = string.Empty;

    /// <summary>
    /// 上傳人員。
    /// </summary>
    [Required(ErrorMessage = "UploadOpe 為必填")]
    public string UploadOpe { get; set; } = string.Empty;
}
