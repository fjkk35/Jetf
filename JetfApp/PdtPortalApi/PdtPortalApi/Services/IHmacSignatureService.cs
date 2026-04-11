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
}