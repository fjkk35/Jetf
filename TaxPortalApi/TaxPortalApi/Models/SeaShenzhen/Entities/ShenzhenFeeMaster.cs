using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.SeaShenzhen.Entities;

/// <summary>
/// 新遞深圳稅金轉檔資料查詢模型。
/// </summary>
[Table("ShenzhenFeeMaster", Schema = "dbo")]
public sealed class ShenzhenFeeMaster
{
    /// <summary>
    /// 資料主鍵。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 資料日期，格式為 yyyyMMdd。
    /// </summary>
    public string? DataDate { get; set; }

    /// <summary>
    /// 物流貨號。
    /// </summary>
    public string? DlvInv { get; set; }

    /// <summary>
    /// 到付款金額。
    /// </summary>
    public int Cod { get; set; }

    /// <summary>
    /// 稅金。
    /// </summary>
    public int Tax { get; set; }

    /// <summary>
    /// 稅金手續費。
    /// </summary>
    public int Fee { get; set; }
}
