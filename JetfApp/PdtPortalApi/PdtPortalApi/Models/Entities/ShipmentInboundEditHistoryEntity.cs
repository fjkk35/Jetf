using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 入庫儲位異動歷程實體。
/// </summary>
[PrimaryKey(nameof(Id))]
[Table("ShipmentInboundEditHistory", Schema = "dbo")]
public sealed class ShipmentInboundEditHistoryEntity
{
    /// <summary>
    /// 歷程 Id。
    /// </summary>
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>
    /// 對應的入庫資料 Id。
    /// </summary>
    [Column("ShipmentInboundId")]
    public int ShipmentInboundId { get; set; }

    /// <summary>
    /// 異動欄位名稱。
    /// </summary>
    [Column("FieldName")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 修改前欄位值。
    /// </summary>
    [Column("OldValue")]
    public string OldValue { get; set; } = string.Empty;

    /// <summary>
    /// 修改後欄位值。
    /// </summary>
    [Column("NewValue")]
    public string NewValue { get; set; } = string.Empty;

    /// <summary>
    /// 修改時間。
    /// </summary>
    [Column("EditTime")]
    public DateTime EditTime { get; set; }

    /// <summary>
    /// 修改人員。
    /// </summary>
    [Column("EditUser")]
    public string EditUser { get; set; } = string.Empty;
}
