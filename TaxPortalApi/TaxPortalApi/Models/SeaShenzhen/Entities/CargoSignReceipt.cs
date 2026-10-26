using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxPortalApi.Models.SeaShenzhen.Entities;

/// <summary>
/// 貨件簽收單圖片資料查詢模型。
/// </summary>
[Table("Cargo_Sign_Receipt", Schema = "dbo")]
public sealed class CargoSignReceipt
{
    /// <summary>
    /// 資料主鍵。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 物流貨號。
    /// </summary>
    [Column("Jetf_Serial")]
    public string? JetfSerial { get; set; }

    /// <summary>
    /// FTP 上的圖片路徑。
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 圖片檔名。
    /// </summary>
    public string? FileName { get; set; }
}
