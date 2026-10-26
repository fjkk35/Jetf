namespace TaxPortalApi.Infrastructure.Options;

/// <summary>
/// 新遞 API 簽名設定。
/// </summary>
public sealed class SeaShenzhenApiOptions
{
    /// <summary>
    /// 設定區段名稱。
    /// </summary>
    public const string SectionName = "SeaShenzhenApi";

    /// <summary>
    /// 與串接方約定的 HMAC-SHA256 金鑰。
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;
}
