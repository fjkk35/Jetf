using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 費用主檔實體。
/// </summary>
[Keyless]
[Table("FEE_MASTER", Schema = "dbo")]
public sealed class FeeMasterEntity
{
    /// <summary>
    /// 單號。
    /// </summary>
    [Column("DLV_INV")]
    public string DlvInv { get; set; } = string.Empty;

    /// <summary>
    /// 下載狀態。
    /// </summary>
    [Column("Download")]
    public string Download { get; set; } = string.Empty;

    /// <summary>
    /// 稅金方式
    /// </summary>
    [Column("INCLUDE_TAX")]
    public string IncludeTax { get; set; } = string.Empty;

    /// <summary>
    /// 稅金一。
    /// </summary>
    [Column("TAX1")]
    public int? Tax1 { get; set; }

    /// <summary>
    /// 稅金二。
    /// </summary>
    [Column("TAX2")]
    public int? Tax2 { get; set; }

    /// <summary>
    /// 報關費。
    /// </summary>
    [Column("CCFEE")]
    public int? Ccfee { get; set; }

    /// <summary>
    /// 到付款。
    /// </summary>
    [Column("COD")]
    public int? Cod { get; set; }
}
