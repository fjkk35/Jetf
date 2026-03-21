using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.TaxDocuments.Entities;

/// <summary>
/// DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 查詢模型。
/// </summary>
[Table("SEA_ORDER_ORIGINAL", Schema = "dbo")]
public sealed class SeaOrderOriginal
{
    /// <summary>
    /// 資料列識別碼。
    /// </summary>
    [Key]
    [Column("ROW_ID")]
    public int RowId { get; set; }

    /// <summary>
    /// JETF 序號。
    /// </summary>
    [Column("JETF_SERIAL")]
    public string JetfSerial { get; set; } = string.Empty;

    /// <summary>
    /// 客戶代碼。
    /// </summary>
    [Column("DESPATCH_NAME")]
    public string CustCode { get; set; } = string.Empty;
}