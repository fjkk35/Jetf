using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// 拆袋上傳資料實體（B6F_UNPACKING_UPLOAD）。
/// </summary>
[Table("B6F_UNPACKING_UPLOAD", Schema = "dbo")]
[PrimaryKey(nameof(MainNumber), nameof(BagNo), nameof(TrackingNo))]
public sealed class B6fUnpackingUploadEntity
{
    /// <summary>
    /// 主號。
    /// </summary>
    [Column("MAINNUMBER")]
    [StringLength(100)]
    public string MainNumber { get; set; } = string.Empty;

    /// <summary>
    /// 袋號。
    /// </summary>
    [Column("BAGNO")]
    [StringLength(100)]
    public string BagNo { get; set; } = string.Empty;

    /// <summary>
    /// 分提單號。
    /// </summary>
    [Column("TRACKINGNO")]
    [StringLength(100)]
    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 備註。
    /// </summary>
    [Column("REMARK")]
    [StringLength(200)]
    public string? Remark { get; set; }

    /// <summary>
    /// 袋號掃描人員。
    /// </summary>
    [Column("SCAN_UPLOAD_OPE")]
    [StringLength(10)]
    public string? ScanUploadOpe { get; set; }

    /// <summary>
    /// 袋號掃描時間。
    /// </summary>
    [Column("SCAN_UPLOAD_TIME", TypeName = "datetime")]
    public DateTime? ScanUploadTime { get; set; }

    /// <summary>
    /// 分提單號掃描人員（空字串代表尚未掃描）。
    /// </summary>
    [Column("SCAN_UPLOAD_OPE2")]
    [StringLength(10)]
    public string? ScanUploadOpe2 { get; set; }

    /// <summary>
    /// 分提單號掃描時間。
    /// </summary>
    [Column("SCAN_UPLOAD_TIME2", TypeName = "datetime")]
    public DateTime? ScanUploadTime2 { get; set; }
}
