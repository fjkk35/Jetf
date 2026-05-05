using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 海運原單實體。
/// </summary>
[Table("SEA_ORDER_ORIGINAL", Schema = "dbo")]
public sealed class SeaOrderOriginalEntity
{
    [Key]
    [Column("ROW_ID")]
    public int Id { get; set; }

    /// <summary>
    /// 主號
    /// </summary>
    [Column("MAINNUMBER")]
    public string MainNumber { get; set; }

    /// <summary>
    /// 分提單號
    /// </summary>
    [Column("BL_NO")]
    public string BlNo { get; set; } = string.Empty;

    /// <summary>
    /// 單號。
    /// </summary>
    [Column("JETF_SERIAL")]
    public string JetfSerial { get; set; } = string.Empty;

    /// <summary>
    /// 進口人或收件人地址。
    /// </summary>
    [Column("IM_ADD")]
    public string ImporterAddr { get; set; } = string.Empty;

    /// <summary>
    /// 進口人或收件人電話。
    /// </summary>
    [Column("IM_PHONENO")]
    public string ImporterPhone { get; set; } = string.Empty;

    /// <summary>
    /// 進口人姓名或收件人名稱。
    /// </summary>
    [Column("IMPORTER")]
    public string Importer { get; set; } = string.Empty;

    /// <summary>
    /// 客戶代碼（CustCode）。
    /// </summary>
    [Column("DESPATCH_NAME")]
    public string CustCode { get; set; } = string.Empty;

    /// <summary>
    /// 承運商名稱。
    /// </summary>
    [Column("TRANS_NAME")]
    public string TransName { get; set; } = string.Empty;

    /// <summary>
    /// 毛重。
    /// </summary>
    [Column("GW")]
    public decimal Gw { get; set; }

    /// <summary>
    /// 到付款。
    /// </summary>
    [Column("CC")]
    public decimal? CC { get; set; }
}
