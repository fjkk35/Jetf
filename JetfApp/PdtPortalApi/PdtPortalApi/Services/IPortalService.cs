using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Enums;
using PdtPortalApi.Models.Requests;

namespace PdtPortalApi.Services;

public interface IPortalService
{
    /// <summary>
    /// 依帳號檢查使用者是否存在。
    /// </summary>
    /// <param name="account">登入帳號。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>存在時回傳 true，否則回傳 false。</returns>
    Task<bool> LoginAsync(string account, CancellationToken cancellationToken);

    /// <summary>
    /// 取得貨件來源清單。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>貨件來源資料。</returns>
    Task<IReadOnlyList<ShipmentInboundSourceTypeDto>> GetShipmentInboundSourcesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 檢查是否存在原始入庫資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="sourceType">貨件來源。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>單號檢查結果。</returns>
    Task<TrackingCheckResult> CheckInboundDataAsync(
        string trackingNo,
        ShipmentInboundSourceType sourceType,
        CancellationToken cancellationToken);

    /// <summary>
    /// 建立入庫資料。
    /// </summary>
    /// <param name="request">入庫請求資料。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>處理結果。</returns>
    Task<ServiceResult> CreateShipmentInboundAsync(CreateShipmentInboundRequest request, CancellationToken cancellationToken);
}
