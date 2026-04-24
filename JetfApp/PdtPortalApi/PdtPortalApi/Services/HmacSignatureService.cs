using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Options;

namespace PdtPortalApi.Services;

public sealed class HmacSignatureService(IOptions<HmacOptions> options, ILogger<HmacSignatureService> logger) : IHmacSignatureService
{
    private readonly HmacOptions _options = options.Value;
    private readonly ILogger<HmacSignatureService> _logger = logger;

    /// <summary>
    /// 驗證時間戳記是否在允許範圍內。
    /// </summary>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    public bool IsTimestampValid(long unixTimeSeconds)
    {
        try
        {
            if (unixTimeSeconds <= 0)
            {
                return false;
            }

            var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
            var delta = DateTimeOffset.UtcNow - requestTime;
            return Math.Abs(delta.TotalMinutes) <= _options.AllowedClockSkewMinutes;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "驗證 Timestamp 發生錯誤，Timestamp: {Timestamp}", unixTimeSeconds);
            return false;
        }
    }

    /// <summary>
    /// 驗證請求簽章是否正確。
    /// </summary>
    /// <param name="request">入庫請求資料。</param>
    /// <param name="unixTimeSeconds">Unix 秒數時間戳記。</param>
    /// <param name="signature">HMAC 簽章字串。</param>
    /// <returns>有效時回傳 true，否則回傳 false。</returns>
    public bool IsSignatureValid(CreateShipmentInboundRequest request, long unixTimeSeconds, string? signature)
    {
        try
        {
            if (!IsTimestampValid(unixTimeSeconds) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(_options.Secret))
            {
                return false;
            }

            var payload = string.Join(
                "\n",
                unixTimeSeconds.ToString(CultureInfo.InvariantCulture),
                request.InboundDate.ToString("O", CultureInfo.InvariantCulture),
                request.TrackingNo ?? string.Empty,
                request.SeqNo ?? string.Empty,
                request.LocationCode ?? string.Empty,
                request.SourceType.ToString(CultureInfo.InvariantCulture),
                request.ReturnTrackingNo ?? string.Empty,
                request.Size ?? string.Empty,
                request.UploadOpe ?? string.Empty);

            var key = Encoding.UTF8.GetBytes(_options.Secret);
            var bytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(key);
            var expectedBytes = hmac.ComputeHash(bytes);

            try
            {
                var providedBytes = Convert.FromHexString(signature.Trim());
                return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
            }
            catch (FormatException exception)
            {
                _logger.LogWarning(exception, "簽章格式不正確，TrackingNo: {TrackingNo}", request.TrackingNo);
                return false;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "驗證 HMAC 簽章發生錯誤，TrackingNo: {TrackingNo}", request.TrackingNo);
            return false;
        }
    }
}
