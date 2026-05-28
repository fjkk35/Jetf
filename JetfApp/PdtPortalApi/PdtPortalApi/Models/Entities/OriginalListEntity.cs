using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 空運原單實體。
/// </summary>
[Table("ORIGINALLIST", Schema = "dbo")]
public sealed class OriginalListEntity
{
    [Key]
    [Column("ROW_ID")]
    public int Id { get; set; }

    /// <summary>
    /// 主號
    /// </summary>
    [Column("MAINNUMBER")]
    public string? MainNumber { get; set; }

    /// <summary>
    /// 提單號或配送單號。
    /// </summary>
    [Column("DELIVERYNO")]
    public string? DeliveryNo { get; set; }

    /// <summary>
    /// 單號。
    /// </summary>
    [Column("TRACKINGNO")]
    public string? TrackingNo { get; set; }

    /// <summary>
    /// 進口人姓名或收件人名稱。
    /// </summary>
    [Column("RECIPIENT")]
    public string? Importer { get; set; }

    /// <summary>
    /// 進口人或收件人電話。
    /// </summary>
    [Column("RECPHONE")]
    public string? ImporterPhone { get; set; }

    /// <summary>
    /// 進口人或收件人地址。
    /// </summary>
    [Column("RECADDRESS")]
    public string? ImporterAddr { get; set; }

    /// <summary>
    /// 客戶代碼（CustCode）。
    /// </summary>
    [Column("DESPATCHNO")]
    public string? CustCode { get; set; }

    /// <summary>
    /// 承運商代號（TransNo）。
    /// </summary>
    [Column("CLEARANCEWAREHOUSING")]
    public int? TransNo { get; set; }

    /// <summary>
    /// 到付款。
    /// </summary>
    [Column("CC")]
    public string? CC { get; set; }
}
