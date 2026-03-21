using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.TaxDocuments.Entities;

/// <summary>
/// DATA_CENTER.dbo.CLEARANCE_TAX 查詢模型。
/// </summary>
[Table("CLEARANCE_TAX", Schema = "dbo")]
public sealed class ClearanceTax
{
    /// <summary>
    /// 資料列識別碼。
    /// </summary>
    [Key]
    [Column("ROW_ID")]
    public int RowId { get; set; }

    /// <summary>
    /// 稅單資料類型。
    /// </summary>
    [Column("DATA_TYPE")]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 合併編號。
    /// </summary>
    [Column("MERGE_NUMBER")]
    public string MergeNumber { get; set; } = string.Empty;

    /// <summary>
    /// 稅單號碼。
    /// </summary>
    [Column("TAX_NUMBER")]
    public string TaxNumber { get; set; } = string.Empty;
}