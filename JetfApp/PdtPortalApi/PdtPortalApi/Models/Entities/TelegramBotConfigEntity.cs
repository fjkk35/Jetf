using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// Telegram Bot 設定實體（TelegramBotConfig，無主鍵僅供查詢）。
/// </summary>
[Table("TelegramBotConfig", Schema = "dbo")]
[Keyless]
public sealed class TelegramBotConfigEntity
{
    /// <summary>
    /// Bot Token。
    /// </summary>
    [Column("BotToken")]
    [StringLength(255)]
    public string BotToken { get; set; } = string.Empty;
}
