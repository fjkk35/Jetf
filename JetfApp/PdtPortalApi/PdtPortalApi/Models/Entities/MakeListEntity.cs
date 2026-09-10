using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 空運製單資料實體。
/// </summary>
[Table("MAKELIST", Schema = "dbo")]
public sealed class MakeListEntity
{
    /// <summary>
    /// 資料主鍵。
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 追蹤單號。
    /// </summary>
    [Column("TRACKINGNO")]
    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 收件人證號。
    /// </summary>
    [Column("RECID")]
    public string RecId { get; set; } = string.Empty;

    /// <summary>
    /// 收件人名稱。
    /// </summary>
    [Column("RECIPIENT")]
    public string Recipient { get; set; } = string.Empty;
}
