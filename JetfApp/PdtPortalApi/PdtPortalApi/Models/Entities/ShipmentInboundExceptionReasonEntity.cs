using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 異常原因實體。
/// </summary>
[Table("ShipmentInboundExceptionReason", Schema = "dbo")]
public sealed class ShipmentInboundExceptionReasonEntity
{
    /// <summary>
    /// 異常原因 Id。
    /// </summary>
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>
    /// 異常原因。
    /// </summary>
    [Column("Reason")]
    public string Reason { get; set; } = string.Empty;
}
