namespace PdtPortalApi.Options;

/// <summary>
/// 拆袋作業設定。
/// </summary>
public sealed class UnpackingOptions
{
    public const string SectionName = "Unpacking";

    /// <summary>
    /// 拆袋備註通知的 Telegram 群組 ChatId（A0006 現場異常查詢）。
    /// </summary>
    public string TelegramChatId { get; set; } = string.Empty;
}
