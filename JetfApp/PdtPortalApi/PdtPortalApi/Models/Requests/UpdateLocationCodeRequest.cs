using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 單件儲位調撥請求。
/// </summary>
public sealed class UpdateLocationCodeRequest
{
    /// <summary>
    /// 流水號。
    /// </summary>
    [Required(ErrorMessage = "SeqNo 為必填")]
    public string SeqNo { get; set; } = string.Empty;

    /// <summary>
    /// 新儲位。
    /// </summary>
    [Required(ErrorMessage = "LocationCode 為必填")]
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>
    /// 修改人員。
    /// </summary>
    [Required(ErrorMessage = "EditUser 為必填")]
    public string EditUser { get; set; } = string.Empty;
}
