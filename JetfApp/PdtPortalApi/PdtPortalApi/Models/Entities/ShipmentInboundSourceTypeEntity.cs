using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 貨件來源主檔實體。
/// </summary>
[Table("ShipmentInboundSourceType", Schema = "dbo")]
public sealed class ShipmentInboundSourceTypeEntity
{
    /// <summary>
    /// 貨件來源識別碼。
    /// </summary>
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>
    /// 貨件來源。
    /// </summary>
    [Column("SourceType")]
    public string SourceType { get; set; } = string.Empty;
}