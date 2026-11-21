using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 拆袋作業-袋號掃描請求。
/// </summary>
public sealed class UnpackingBagNoRequest
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
    /// 上傳操作人員帳號。
    /// </summary>
    [Required(ErrorMessage = "UploadOpe 為必填")]
    public string UploadOpe { get; set; } = string.Empty;
}
