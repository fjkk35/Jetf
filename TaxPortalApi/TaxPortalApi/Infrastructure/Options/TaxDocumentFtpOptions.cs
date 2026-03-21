using System.ComponentModel.DataAnnotations;

namespace TaxPortalApi.Infrastructure.Options;

/// <summary>
/// 稅金單 FTP 連線設定。
/// </summary>
public sealed class TaxDocumentFtpOptions
{
    /// <summary>
    /// 設定區段名稱。
    /// </summary>
    public const string SectionName = "TaxDocumentFtp";

    /// <summary>
    /// FTP 主機位址。
    /// </summary>
    [Required]
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// FTP 使用者帳號。
    /// </summary>
    [Required]
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// FTP 使用者密碼。
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}