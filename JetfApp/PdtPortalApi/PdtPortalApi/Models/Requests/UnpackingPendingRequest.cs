using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 拆袋作業-查詢袋號尚未掃描的分提單號請求。
/// </summary>
public sealed class UnpackingPendingRequest
{
    /// <summary>
    /// 作業地區（FTZ、TACT）。
    /// </summary>
    [Required(ErrorMessage = "DataType 為必填")]
    [AllowedValues("FTZ", "TACT", ErrorMessage = "DataType 僅允許 FTZ 或 TACT")]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 袋號。
    /// </summary>
    [Required(ErrorMessage = "BagNo 為必填")]
    public string BagNo { get; set; } = string.Empty;

    /// <summary>
    /// 操作人員帳號（僅供日誌使用）。
    /// </summary>
    public string? UploadOpe { get; set; }
}
