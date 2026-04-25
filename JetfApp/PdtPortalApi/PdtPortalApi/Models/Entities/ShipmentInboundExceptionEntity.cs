using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 入庫異常件實體。
/// </summary>
[PrimaryKey(nameof(ShipmentInboundId), nameof(FilePath))]
[Table("ShipmentInboundException", Schema = "dbo")]
public sealed class ShipmentInboundExceptionEntity
{
    /// <summary>
    /// 對應的入庫資料 Id。
    /// </summary>
    [Column("ShipmentInboundId")]
    public int ShipmentInboundId { get; set; }

    /// <summary>
    /// 流水號。
    /// </summary>
    [Column("SeqNo")]
    public string SeqNo { get; set; } = string.Empty;

    /// <summary>
    /// 異常原因。
    /// </summary>
    [Column("Reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 照片路徑。
    /// </summary>
    [Column("FilePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 上傳人員。
    /// </summary>
    [Column("UploadOpe")]
    public string UploadOpe { get; set; } = string.Empty;
}
