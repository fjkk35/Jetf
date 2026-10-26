using TaxPortalApi.Models.SeaShenzhen;

namespace TaxPortalApi.Services.Interfaces;

/// <summary>
/// 新遞簽名驗證、金額與簽收單查詢服務。
/// </summary>
public interface ISeaShenzhenService
{
    /// <summary>
    /// 驗證時間戳與 HMAC-SHA256 簽章；失敗時拋出 API 例外。
    /// </summary>
    /// <param name="request">物流貨號與簽名參數。</param>
    void ValidateSignature(SeaShenzhenQueryRequest request);

    /// <summary>
    /// 依物流貨號查詢新遞金額。
    /// </summary>
    /// <param name="jetfSerial">物流貨號。</param>
    /// <returns>物流貨號、到付款、稅金、稅金手續費及原始支付方式。</returns>
    Task<SeaShenzhenAmountResponse> GetAmountAsync(string jetfSerial);

    /// <summary>
    /// 確認新遞原單存在後，查詢簽收單並回傳 Base64 圖片。
    /// </summary>
    /// <param name="jetfSerial">物流貨號。</param>
    /// <returns>物流貨號與所有可讀取的簽收圖片。</returns>
    Task<SeaShenzhenSignReceiptResponse> GetSignReceiptsAsync(string jetfSerial);
}
