namespace TaxPortalApi.Services.Interfaces;

/// <summary>
/// 簽收單圖片檔案讀取服務。
/// </summary>
public interface ICargoSignReceiptFileService
{
    /// <summary>
    /// 讀取 FTP 圖片；檔案不存在或內容為空時回傳 null。
    /// </summary>
    /// <param name="filePath">簽收單圖片的 FTP 檔案路徑。</param>
    /// <returns>圖片位元組；無可用內容時回傳 null。</returns>
    Task<byte[]?> ReadAsync(string filePath);
}
