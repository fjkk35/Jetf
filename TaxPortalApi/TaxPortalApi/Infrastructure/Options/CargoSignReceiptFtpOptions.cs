namespace TaxPortalApi.Infrastructure.Options;

/// <summary>
/// 簽收單圖片 FTP 連線設定，使用既有簽收單 FTP 帳號。
/// </summary>
public sealed class CargoSignReceiptFtpOptions
{
    /// <summary>
    /// 設定區段名稱。
    /// </summary>
    public const string SectionName = "CargoSignReceiptFtp";

    /// <summary>
    /// FTP 主機位址。
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// FTP 帳號。
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// FTP 密碼。
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
