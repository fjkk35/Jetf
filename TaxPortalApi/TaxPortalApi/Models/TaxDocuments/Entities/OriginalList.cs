using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.TaxDocuments.Entities;

/// <summary>
/// DATA_CENTER.dbo.ORIGINALLIST 查詢模型。
/// </summary>
[Table("ORIGINALLIST", Schema = "dbo")]
public sealed class OriginalList
{
    /// <summary>
    /// 資料列識別碼。
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 配送單號。
    /// </summary>
    [Column("DELIVERYNO")]
    public string DeliveryNo { get; set; } = string.Empty;

    /// <summary>
    /// 客戶代碼。
    /// </summary>
    [Column("DESPATCHNO")]
    public string CustCode { get; set; } = string.Empty;
}