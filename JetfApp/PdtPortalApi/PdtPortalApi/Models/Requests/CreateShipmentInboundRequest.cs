using System.ComponentModel.DataAnnotations;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 入庫資料建立請求。
/// </summary>
public sealed class CreateShipmentInboundRequest
{
    /// <summary>
    /// 入庫日期。
    /// </summary>
    [Required(ErrorMessage = "InboundDate 為必填")]
    public DateTimeOffset InboundDate { get; set; }

    /// <summary>
    /// 單號。
    /// </summary>
    [Required(ErrorMessage = "TrackingNo 為必填")]
    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 流水號。
    /// </summary>
    [Required(ErrorMessage = "SeqNo 為必填")]
    public string SeqNo { get; set; } = string.Empty;

    /// <summary>
    /// 儲位。
    /// </summary>
    [Required(ErrorMessage = "LocationCode 為必填")]
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>
    /// 貨件來源。
    /// </summary>
    public byte SourceType { get; set; }

    /// <summary>
    /// 退回的追蹤編號（若為退貨或重出時使用）。
    /// </summary>
    public string ReturnTrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 尺寸。
    /// </summary>
    public string Size { get; set; } = "小";

    /// <summary>
    /// 上傳作業人員帳號。
    /// </summary>
    [Required(ErrorMessage = "UploadOpe 為必填")]
    public string UploadOpe { get; set; } = string.Empty;
}
