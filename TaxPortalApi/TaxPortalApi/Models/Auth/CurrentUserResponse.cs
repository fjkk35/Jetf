namespace TaxPortalApi.Models.Auth;

/// <summary>
/// 目前登入使用者資訊。
/// </summary>
public sealed class CurrentUserResponse
{
    /// <summary>
    /// 使用者識別碼。
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 使用者帳號。
    /// </summary>
    public string UserName { get; init; } = string.Empty;
}