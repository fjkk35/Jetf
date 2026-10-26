using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.SeaShenzhen.Entities;

/// <summary>
/// 新遞深圳原始託運資料查詢模型。
/// </summary>
[Table("SeaShenzhenOriginal", Schema = "dbo")]
public sealed class SeaShenzhenOriginal
{
    /// <summary>
    /// 資料主鍵。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 資料日期。
    /// </summary>
    public DateTime DataDate { get; set; }

    /// <summary>
    /// 物流貨號。
    /// </summary>
    public string? JetfSerial { get; set; }

    /// <summary>
    /// 原始代收金額。
    /// </summary>
    public double? Cc { get; set; }

    /// <summary>
    /// 稅金支付方式原始值。
    /// </summary>
    public string? TaxPayment { get; set; }
}
