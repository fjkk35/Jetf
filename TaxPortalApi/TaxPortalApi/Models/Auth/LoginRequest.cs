using System.ComponentModel.DataAnnotations;

namespace TaxPortalApi.Models.Auth;

/// <summary>
/// 登入請求資料。
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 登入帳號。
    /// </summary>
    [Required(ErrorMessage = "請輸入帳號")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登入密碼。
    /// </summary>
    [Required(ErrorMessage = "請輸入密碼")]
    public string Password { get; set; } = string.Empty;
}