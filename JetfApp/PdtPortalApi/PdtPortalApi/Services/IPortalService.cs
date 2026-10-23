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
    /// <returns>存在時回傳 true，否則回傳 false。</returns>
    Task<bool> LoginAsync(string account);

    /// <summary>
    /// 取得貨件來源清單。
    /// </summary>
    /// <returns>貨件來源資料。</returns>
    Task<IReadOnlyList<ShipmentInboundSourceTypeDto>> GetShipmentInboundSourcesAsync();

    /// <summary>
    /// 檢查是否存在原始入庫資料。
    /// </summary>
    /// <param name="trackingNo">單號。</param>
    /// <param name="sourceType">貨件來源。</param>
    /// <returns>單號檢查結果。</returns>
    Task<TrackingCheckResult> CheckInboundDataAsync(
        string trackingNo,
        ShipmentInboundSourceType sourceType);

    /// <summary>
    /// 建立入庫資料。
    /// </summary>
    /// <param name="request">入庫請求資料。</param>
    /// <returns>處理結果。</returns>
    Task<ServiceResult> CreateShipmentInboundAsync(CreateShipmentInboundRequest request);

    /// <summary>
    /// 取得異常原因清單。
    /// </summary>
    /// <returns>異常原因資料。</returns>
    Task<IReadOnlyList<ShipmentInboundExceptionReasonDto>> GetShipmentInboundExceptionReasonsAsync();

    /// <summary>
    /// 建立異常件資料。
    /// </summary>
    /// <param name="request">異常件請求資料。</param>
    /// <returns>處理結果。</returns>
    Task<ServiceResult> CreateShipmentInboundExceptionAsync(
        CreateShipmentInboundExceptionRequest request);

    /// <summary>
    /// 更新單件儲位。
    /// </summary>
    /// <param name="request">儲位調撥請求。</param>
    /// <returns>處理結果。</returns>
    Task<ServiceResult> UpdateLocationCodeAsync(
        UpdateLocationCodeRequest request);

    /// <summary>
    /// 取得整板儲位調撥件數。
    /// </summary>
    /// <param name="request">件數查詢請求。</param>
    /// <returns>處理結果。</returns>
    Task<ServiceResult> GetBatchLocationUpdateCountAsync(
        GetBatchLocationUpdateCountRequest request);

    /// <summary>
    /// 執行整板儲位調撥。
    /// </summary>
    /// <param name="request">整板調撥請求。</param>
    /// <returns>處理結果。</returns>
    Task<ServiceResult> BatchUpdateLocationCodeAsync(
        BatchUpdateLocationCodeRequest request);
}
