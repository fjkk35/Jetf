using System.ComponentModel.DataAnnotations;
using PdtPortalApi.Models.Enums;

namespace PdtPortalApi.Models.Requests;

/// <summary>
/// 單號查詢請求。
/// </summary>
public sealed class TrackingNoRequest
{
    /// <summary>
    /// 單號。
    /// </summary>
    [Required(ErrorMessage = "TrackingNo 為必填")]
    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>
    /// 貨件來源。
    /// </summary>
    [EnumDataType(typeof(ShipmentInboundSourceType), ErrorMessage = "SourceType 不在有效範圍")]
    public ShipmentInboundSourceType SourceType { get; set; }
}
