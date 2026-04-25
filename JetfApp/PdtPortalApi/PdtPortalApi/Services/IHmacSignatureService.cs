using PdtPortalApi.Models.Requests;

namespace PdtPortalApi.Services;

public interface IHmacSignatureService
{
    /// <summary>
    /// 驗證時間戳記是否在允許範圍內。
    /// </summary>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    bool IsTimestampValid(long unixTimeSeconds);

    /// <summary>
    /// 驗證請求簽章是否正確。
    /// </summary>
    /// <param name="request">入庫請求資料。</param>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <param name="signature">HMAC 簽章字串。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    bool IsSignatureValid(CreateShipmentInboundRequest request, long unixTimeSeconds, string? signature);

    /// <summary>
    /// 驗證異常件請求簽章是否正確。
    /// </summary>
    /// <param name="request">異常件請求資料。</param>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <param name="signature">HMAC 簽章字串。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    bool IsSignatureValid(CreateShipmentInboundExceptionRequest request, long unixTimeSeconds, string? signature);

    /// <summary>
    /// 驗證單件儲位調撥請求簽章是否正確。
    /// </summary>
    /// <param name="request">單件儲位調撥請求資料。</param>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <param name="signature">HMAC 簽章字串。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    bool IsSignatureValid(UpdateLocationCodeRequest request, long unixTimeSeconds, string? signature);

    /// <summary>
    /// 驗證整板儲位調撥件數查詢請求簽章是否正確。
    /// </summary>
    /// <param name="request">整板儲位調撥件數查詢請求資料。</param>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <param name="signature">HMAC 簽章字串。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    bool IsSignatureValid(GetBatchLocationUpdateCountRequest request, long unixTimeSeconds, string? signature);

    /// <summary>
    /// 驗證整板儲位調撥請求簽章是否正確。
    /// </summary>
    /// <param name="request">整板儲位調撥請求資料。</param>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <param name="signature">HMAC 簽章字串。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    bool IsSignatureValid(BatchUpdateLocationCodeRequest request, long unixTimeSeconds, string? signature);
}
