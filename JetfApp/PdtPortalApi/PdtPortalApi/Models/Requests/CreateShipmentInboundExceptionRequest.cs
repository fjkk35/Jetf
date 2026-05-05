using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 建立異常件請求資料。
/// </summary>
public sealed class CreateShipmentInboundExceptionRequest
{
    /// <summary>
    /// 流水號。
    /// </summary>
    [Required(ErrorMessage = "SeqNo 為必填")]
    public string SeqNo { get; set; } = string.Empty;

    /// <summary>
    /// 異常原因 Id。
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ExceptionReasonId 為必填")]
    public int ExceptionReasonId { get; set; }

    /// <summary>
    /// 照片 Base64。
    /// </summary>
    [Required(ErrorMessage = "Photo 為必填")]
    public string Photo { get; set; } = string.Empty;

    /// <summary>
    /// 上傳操作人員帳號或識別。
    /// </summary>
    [Required(ErrorMessage = "UploadOpe 為必填")]
    public string UploadOpe { get; set; } = string.Empty;
}
