using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace TaxPortalApi.Models.SeaShenzhen;

/// <summary>
/// 新遞金額及簽收單共用查詢參數。
/// </summary>
public sealed class SeaShenzhenQueryRequest
{
    /// <summary>
    /// 物流貨號，前後不可包含空白。
    /// </summary>
    [Required(ErrorMessage = "物流貨號必填")]
    [FromQuery(Name = "trackinngNo")]
    [StringLength(20, ErrorMessage = "物流貨號最多 20 字元")]
    public string TrackinngNo { get; init; } = string.Empty;

    /// <summary>
    /// 呼叫當下的 UTC Unix Timestamp，單位為秒，有效時間為 180 秒。
    /// </summary>
    [Required(ErrorMessage = "Timestamp 必填")]
    [Range(1, long.MaxValue, ErrorMessage = "Timestamp 格式錯誤")]
    [FromQuery(Name = "timestamp")]
    public long Timestamp { get; init; }

    /// <summary>
    /// HMAC-SHA256 簽章，64 個小寫十六進位字元。
    /// </summary>
    [Required(ErrorMessage = "Sign 必填")]
    [FromQuery(Name = "sign")]
    public string Sign { get; init; } = string.Empty;
}
