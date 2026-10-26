using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using TaxPortalApi.Infrastructure.Exceptions;
using TaxPortalApi.Infrastructure.Options;
using TaxPortalApi.Infrastructure.Persistence;
using TaxPortalApi.Models.SeaShenzhen;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Services;

/// <summary>
/// 新遞簽名驗證、金額與簽收單查詢服務。
/// </summary>
public sealed class SeaShenzhenService(
    JetfDbContext jetfDbContext,
    ICargoSignReceiptFileService receiptFileService,
    IOptions<SeaShenzhenApiOptions> options,
    TimeProvider timeProvider) : ISeaShenzhenService
{
    /// <summary>
    /// 驗證物流貨號、180 秒有效時間與 HMAC-SHA256 簽章。
    /// </summary>
    /// <param name="request">物流貨號、UTC Unix 時間戳與小寫十六進位簽章。</param>
    /// <exception cref="ApiException">參數或簽章不符、時間戳超出有效範圍，或簽名金鑰尚未設定。</exception>
    public void ValidateSignature(SeaShenzhenQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrackinngNo) || request.TrackinngNo != request.TrackinngNo.Trim())
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "物流貨號格式錯誤");
        }

        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            throw new ApiException(StatusCodes.Status503ServiceUnavailable, "簽名金鑰未設定");
        }

        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        // 只接受最近 180 秒的請求；未來時間戳也不接受。
        if (request.Timestamp < now - 180 || request.Timestamp > now)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Timestamp 超出有效時間");
        }

        if (request.Sign.Length != 64 || request.Sign.Any(character =>
                character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Sign 驗證失敗");
        }

        // 依文件約定的欄位順序重算簽章，使用固定時間比較避免洩漏比對進度。
        var payload = $"trackinngNo={request.TrackinngNo}&timestamp={request.Timestamp.ToString(CultureInfo.InvariantCulture)}";
        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(options.Value.SecretKey),
            Encoding.UTF8.GetBytes(payload));
        var supplied = Convert.FromHexString(request.Sign);
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Sign 驗證失敗");
        }
    }

    /// <summary>
    /// 依物流貨號查詢新遞原單與稅金轉檔資料，取得金額及支付方式。
    /// </summary>
    /// <param name="jetfSerial">物流貨號，使用完整值比對。</param>
    /// <returns>到付款、稅金、稅金手續費與原始支付方式；未取得的金額回傳 0。</returns>
    /// <exception cref="ApiException">查無此物流貨號的新遞原始託運資料。</exception>
    public async Task<SeaShenzhenAmountResponse> GetAmountAsync(string jetfSerial)
    {
        var original = await jetfDbContext.SeaShenzhenOriginals
            .AsNoTracking()
            .Where(item => item.JetfSerial == jetfSerial)
            .OrderByDescending(item => item.DataDate)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync();

        if (original is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "查無託運資料");
        }

        var fee = await jetfDbContext.ShenzhenFeeMasters
            .AsNoTracking()
            .Where(item => item.DlvInv == original.JetfSerial)
            .OrderByDescending(item => item.DataDate)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync();

        // 比照 SeaShenzhenOriginalQuery：有轉檔資料取 Cod；否則取原單 Cc。
        double cod = 0;
        if (fee is not null)
        {
            cod = fee.Cod;
        }
        else if (original.Cc.HasValue)
        {
            cod = original.Cc.Value;
        }

        // 沒有轉檔資料時，稅金與手續費回傳 0。
        return new SeaShenzhenAmountResponse
        {
            TrackinngNo = original.JetfSerial ?? jetfSerial,
            Cod = cod,
            Tax = fee?.Tax ?? 0,
            Fee = fee?.Fee ?? 0,
            TaxPayment = original.TaxPayment
        };
    }

    /// <summary>
    /// 確認新遞原單存在後，查詢簽收單並將可讀取的圖片轉成 Base64。
    /// </summary>
    /// <param name="jetfSerial">物流貨號，對應簽收單資料表的 Jetf_Serial。</param>
    /// <returns>物流貨號與簽收圖片清單，支援同一貨號有多張圖片。</returns>
    /// <exception cref="ApiException">查無新遞原單、沒有可讀取圖片，或 FTP 讀取失敗。</exception>
    public async Task<SeaShenzhenSignReceiptResponse> GetSignReceiptsAsync(string jetfSerial)
    {
        // 必須先確認新遞原單存在，才能查詢簽收單資料表或讀取 FTP。
        var hasOriginal = await jetfDbContext.SeaShenzhenOriginals
            .AsNoTracking()
            .AnyAsync(item => item.JetfSerial == jetfSerial);
        if (!hasOriginal)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "查無託運資料");
        }

        var receipts = await jetfDbContext.CargoSignReceipts
            .AsNoTracking()
            .Where(item => item.JetfSerial == jetfSerial)
            .OrderBy(item => item.Id)
            .ToListAsync();
        var images = new List<SeaShenzhenSignReceiptImage>();
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        // 略過未填路徑或檔案不存在的紀錄，回傳所有成功讀取的簽收圖片。
        foreach (var receipt in receipts)
        {
            if (string.IsNullOrWhiteSpace(receipt.FilePath))
            {
                continue;
            }

            var content = await receiptFileService.ReadAsync(receipt.FilePath);
            if (content is null || content.Length == 0)
            {
                continue;
            }

            var fileName = string.IsNullOrWhiteSpace(receipt.FileName)
                ? Path.GetFileName(receipt.FilePath.Replace('\\', '/'))
                : receipt.FileName;
            contentTypeProvider.TryGetContentType(fileName, out var contentType);
            images.Add(new SeaShenzhenSignReceiptImage
            {
                FileName = fileName,
                ContentType = contentType ?? "application/octet-stream",
                ContentBase64 = Convert.ToBase64String(content)
            });
        }

        if (images.Count == 0)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "查無簽收單圖片");
        }

        return new SeaShenzhenSignReceiptResponse { TrackinngNo = jetfSerial, Images = images };
    }
}
