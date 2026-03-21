using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.TaxDocuments.Entities;

/// <summary>
/// jetf.dbo.Clearance_Tax_Pdf 查詢模型。
/// </summary>
[Table("Clearance_Tax_Pdf", Schema = "dbo")]
public sealed class ClearanceTaxPdf
{
    /// <summary>
    /// 資料列識別碼。
    /// </summary>
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>
    /// 稅單號碼。
    /// </summary>
    [Column("TaxNumber")]
    public string TaxNumber { get; set; } = string.Empty;

    /// <summary>
    /// FTP 檔案路徑。
    /// </summary>
    [Column("FilePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 檔案名稱。
    /// </summary>
    [Column("FileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間。
    /// </summary>
    [Column("CrtDateTime")]
    public DateTime? CreatedAt { get; set; }
}