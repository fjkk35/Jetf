using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 應向物流代收的到付款資料。
/// </summary>
[Table("FEE_MASTER_COD", Schema = "dbo")]
public sealed class FeeMasterCodEntity
{
    /// <summary>
    /// 到付款資料識別碼。
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 物流貨號。
    /// </summary>
    [Column("DLV_INV")]
    public string? DlvInv { get; set; }

    /// <summary>
    /// 出倉時間。
    /// </summary>
    [Column("SIGN_OUT_TIME")]
    public DateTime SignOutTime { get; set; }

    /// <summary>
    /// 應向物流代收金額。
    /// </summary>
    public int? ToDlvCod { get; set; }
}
