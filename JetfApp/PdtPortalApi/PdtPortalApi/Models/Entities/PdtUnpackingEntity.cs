using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PdtPortalApi.Models.Entities;

/// <summary>
/// PDT 拆袋 API 紀錄實體（PdtUnpacking）。
/// </summary>
/// <remarks>
/// 資料表沒有主鍵，此處的 PrimaryKey 僅供 EF 追蹤新增資料使用，只做 Insert 不做查詢更新。
/// CrtDateTime 由 DB 預設值 getdate() 產生，故不對應。
/// </remarks>
[Table("PdtUnpacking", Schema = "dbo")]
[PrimaryKey(nameof(Api), nameof(BagNo), nameof(UploadOpe), nameof(UploadTime))]
public sealed class PdtUnpackingEntity
{
    /// <summary>
    /// API 名稱（BagNo、TrackingNo）。
    /// </summary>
    [Column("API")]
    [StringLength(30)]
    public string Api { get; set; } = string.Empty;

    /// <summary>
    /// 作業地區。
    /// </summary>
    [Column("DataType")]
    [StringLength(100)]
    public string? DataType { get; set; }

    /// <summary>
    /// 袋號。
    /// </summary>
    [Column("BagNo")]
    [StringLength(100)]
    public string BagNo { get; set; } = string.Empty;

    /// <summary>
    /// 分提單號。
    /// </summary>
    [Column("TrackingNo")]
    [StringLength(100)]
    public string? TrackingNo { get; set; }

    /// <summary>
    /// 上傳人員。
    /// </summary>
    [Column("UploadOpe")]
    [StringLength(10)]
    public string UploadOpe { get; set; } = string.Empty;

    /// <summary>
    /// 上傳時間。
    /// </summary>
    [Column("UploadTime", TypeName = "datetime")]
    public DateTime UploadTime { get; set; }
}
