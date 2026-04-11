using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// App 版本檢查請求。
/// </summary>
public sealed class AppVersionCheckRequest
{
    /// <summary>
    /// App 當前版本號。
    /// </summary>
    [Required(ErrorMessage = "VersionCode 為必填")]
    public string VersionCode { get; set; } = string.Empty;
}