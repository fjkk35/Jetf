using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 取得整板調撥件數請求。
/// </summary>
public sealed class GetBatchLocationUpdateCountRequest
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
}
