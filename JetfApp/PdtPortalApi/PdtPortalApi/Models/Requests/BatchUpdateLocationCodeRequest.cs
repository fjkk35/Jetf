using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 整板儲位調撥請求。
/// </summary>
public sealed class BatchUpdateLocationCodeRequest
{
    /// <summary>
    /// 原儲位。
    /// </summary>
    [Required(ErrorMessage = "OldLocationCode 為必填")]
    public string OldLocationCode { get; set; } = string.Empty;

    /// <summary>
    /// 新儲位。
    /// </summary>
    [Required(ErrorMessage = "NewLocationCode 為必填")]
    public string NewLocationCode { get; set; } = string.Empty;

    /// <summary>
    /// 修改人員。
    /// </summary>
    [Required(ErrorMessage = "EditUser 為必填")]
    public string EditUser { get; set; } = string.Empty;
}
