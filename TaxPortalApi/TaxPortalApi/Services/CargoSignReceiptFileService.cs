using FluentFTP;
using Microsoft.Extensions.Options;
using TaxPortalApi.Infrastructure.Exceptions;
using TaxPortalApi.Infrastructure.Options;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Services;

/// <summary>
/// 從簽收單 FTP 讀取圖片。
/// </summary>
public sealed class CargoSignReceiptFileService(
    IOptions<CargoSignReceiptFtpOptions> options,
    ILogger<CargoSignReceiptFileService> logger) : ICargoSignReceiptFileService
{
    /// <summary>
    /// 依簽收單資料表的檔案路徑，從設定的 FTP 下載圖片內容。
    /// </summary>
    /// <param name="filePath">簽收單圖片在 FTP 上的檔案路徑。</param>
    /// <returns>圖片位元組；檔案不存在或內容為空時回傳 null。</returns>
    /// <exception cref="ApiException">FTP 尚未設定，或連線、下載失敗。</exception>
    public async Task<byte[]?> ReadAsync(string filePath)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.UserName)
            || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new ApiException(StatusCodes.Status503ServiceUnavailable, "簽收單 FTP 未設定");
        }

        // 路徑來自簽收單資料表，呼叫端只能傳入物流貨號。
        var remotePath = filePath.Replace('\\', '/').Trim();
        if (!remotePath.StartsWith('/'))
        {
            remotePath = "/" + remotePath;
        }

        try
        {
            await using var client = new AsyncFtpClient(settings.Host, settings.UserName, settings.Password);
            await client.AutoConnect();
            if (!await client.FileExists(remotePath))
            {
                return null;
            }

            var bytes = await client.DownloadBytes(remotePath, restartPosition: 0);
            return bytes.Length == 0 ? null : bytes;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "讀取簽收單 FTP 圖片失敗");
            throw new ApiException(StatusCodes.Status502BadGateway, "簽收單圖片讀取失敗");
        }
    }
}
