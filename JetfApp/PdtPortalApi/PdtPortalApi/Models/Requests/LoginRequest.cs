using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 登入請求。
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// 使用者帳號。
    /// </summary>
    [Required(ErrorMessage = "Account 為必填")]
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// App 當前版本號。
    /// </summary>
    [Required(ErrorMessage = "VersionCode 為必填")]
    public string VersionCode { get; set; } = string.Empty;
}