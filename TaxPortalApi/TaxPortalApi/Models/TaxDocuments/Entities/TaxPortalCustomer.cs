using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.TaxDocuments.Entities;

/// <summary>
/// jetf.dbo.TaxPortalCustomer 查詢模型。
/// </summary>
[Table("TaxPortalCustomer", Schema = "dbo")]
public sealed class TaxPortalCustomer
{
    /// <summary>
    /// 資料列識別碼。
    /// </summary>
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>
    /// Tax Portal 使用者識別碼。
    /// </summary>
    [Column("TaxPortalUserId")]
    public int TaxPortalUserId { get; set; }

    /// <summary>
    /// 客戶代碼。
    /// </summary>
    [Column("CustCode")]
    public string CustCode { get; set; } = string.Empty;
}