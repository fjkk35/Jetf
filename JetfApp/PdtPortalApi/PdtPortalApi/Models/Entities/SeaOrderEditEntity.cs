using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 海運製單資料實體。
/// </summary>
[Table("SEA_ORDER_EDIT", Schema = "dbo")]
public sealed class SeaOrderEditEntity
{
    /// <summary>
    /// 資料流水號。
    /// </summary>
    [Key]
    [Column("ROW_ID")]
    public int Id { get; set; }

    /// <summary>
    /// 物流貨號。
    /// </summary>
    [Column("JETF_SERIAL")]
    public string JetfSerial { get; set; } = string.Empty;

    /// <summary>
    /// 進口人證號。
    /// </summary>
    [Column("IMPORTER_ID")]
    public string ImporterId { get; set; } = string.Empty;

    /// <summary>
    /// 進口人名稱。
    /// </summary>
    [Column("IMPORTER")]
    public string Importer { get; set; } = string.Empty;

    /// <summary>
    /// 毛重。
    /// </summary>
    [Column("GW")]
    public decimal? Gw { get; set; }
}
