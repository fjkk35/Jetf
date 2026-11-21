using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PdtPortalApi.Data;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Entities;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Options;

namespace PdtPortalApi.Services;

/// <summary>
/// 拆袋作業服務，邏輯移植自 PDTWebAPI UnpackingService 的 TACT / FTZ 分支。
/// </summary>
public sealed class UnpackingService(
    JetfDbContext jetfDbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<UnpackingOptions> unpackingOptions,
    ILogger<UnpackingService> logger) : IUnpackingService
{
    public const string TelegramHttpClientName = "Telegram";

    private readonly JetfDbContext _jetfDbContext = jetfDbContext;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly UnpackingOptions _unpackingOptions = unpackingOptions.Value;
    private readonly ILogger<UnpackingService> _logger = logger;

    /// <summary>
    /// 掃描袋號，判斷是否需要拆袋。
    /// 會先寫入 PdtUnpacking 紀錄，查詢袋號尚未掃描的分提單號；
    /// 有資料即需拆袋，備註不為空時發送 Telegram 通知，並更新袋號掃描人員與時間。
    /// </summary>
    /// <param name="request">袋號掃描請求。</param>
    /// <returns>是否需要拆袋與尚未掃描的分提單號。</returns>
    public async Task<UnpackingBagResultDto> ScanBagNoAsync(UnpackingBagNoRequest request)
    {
        try
        {
            var dataType = request.DataType.Trim().ToUpperInvariant();
            var bagNo = request.BagNo.Trim();
            var uploadOpe = request.UploadOpe.Trim();
            var uploadTime = GetUploadTime();

            await InsertPdtUnpackingAsync("BagNo", dataType, bagNo, null, uploadOpe, uploadTime);

            var rows = await GetPendingRowsAsync(bagNo);
            if (rows.Count == 0)
            {
                return new UnpackingBagResultDto { NeedUnpacking = false };
            }

            // 有備註需要發送 Telegram 通知
            var remarks = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Remark))
                .ToList();
            if (remarks.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"拆袋處理：{dataType}");
                sb.AppendLine($"袋號：{bagNo}");
                sb.AppendLine("分提單號：");
                foreach (var remark in remarks)
                {
                    sb.AppendLine(remark.TrackingNo?.Trim());
                    sb.AppendLine(remark.Remark?.Trim());
                }

                await SendTelegramAsync(sb.ToString());
            }

            // 更新袋號掃描人員與時間
            await _jetfDbContext.B6fUnpackingUploads
                .Where(entity => entity.BagNo == bagNo)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(entity => entity.ScanUploadOpe, uploadOpe)
                    .SetProperty(entity => entity.ScanUploadTime, uploadTime));

            return new UnpackingBagResultDto
            {
                NeedUnpacking = true,
                PendingTrackingNos = ToTrackingNos(rows)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "拆袋袋號掃描失敗，BagNo: {BagNo}", request.BagNo);
            throw;
        }
    }

    /// <summary>
    /// 掃描分提單號，判斷是否為此袋需拆出的貨件。
    /// 會先寫入 PdtUnpacking 紀錄；符合袋號與分提單號時回傳備註（無備註回傳「拆出」），
    /// 並更新分提單號掃描人員與時間。
    /// </summary>
    /// <param name="request">分提單號掃描請求。</param>
    /// <returns>是否需拆出與拆出提示訊息。</returns>
    public async Task<UnpackingTrackingResultDto> ScanTrackingNoAsync(UnpackingTrackingNoRequest request)
    {
        try
        {
            var dataType = request.DataType.Trim().ToUpperInvariant();
            var bagNo = request.BagNo.Trim();
            var trackingNo = request.TrackingNo.Trim();
            var uploadOpe = request.UploadOpe.Trim();
            var uploadTime = GetUploadTime();

            await InsertPdtUnpackingAsync("TrackingNo", dataType, bagNo, trackingNo, uploadOpe, uploadTime);

            var matched = await _jetfDbContext.B6fUnpackingUploads
                .AsNoTracking()
                .Where(entity => entity.BagNo == bagNo && entity.TrackingNo == trackingNo)
                .Select(entity => new { entity.Remark })
                .FirstOrDefaultAsync();

            var isUnpackTarget = matched is not null;
            var resultMessage = string.Empty;
            if (matched is not null)
            {
                // 備註不是空白，顯示備註訊息
                var remark = matched.Remark?.Trim();
                resultMessage = string.IsNullOrEmpty(remark) ? "拆出" : remark;

                // 更新分提單號掃描人員與時間
                await _jetfDbContext.B6fUnpackingUploads
                    .Where(entity => entity.BagNo == bagNo && entity.TrackingNo == trackingNo)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(entity => entity.ScanUploadOpe2, uploadOpe)
                        .SetProperty(entity => entity.ScanUploadTime2, uploadTime));
            }

            return new UnpackingTrackingResultDto
            {
                IsUnpackTarget = isUnpackTarget,
                ResultMessage = resultMessage
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "拆袋分提單號掃描失敗，BagNo: {BagNo}, TrackingNo: {TrackingNo}",
                request.BagNo,
                request.TrackingNo);
            throw;
        }
    }

    /// <summary>
    /// 取得袋號尚未掃描的分提單號，供 APP 返回時再次確認是否全部掃描完成。
    /// </summary>
    /// <param name="bagNo">袋號。</param>
    /// <returns>尚未掃描的分提單號；全部掃描完成時為空清單。</returns>
    public async Task<IReadOnlyList<string>> GetPendingTrackingNosAsync(string bagNo)
    {
        try
        {
            return ToTrackingNos(await GetPendingRowsAsync(bagNo.Trim()));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢拆袋未掃描分提單號失敗，BagNo: {BagNo}", bagNo);
            throw;
        }
    }

    /// <summary>
    /// 查詢袋號尚未掃描（SCAN_UPLOAD_OPE2 為空字串）的拆袋資料。
    /// </summary>
    /// <param name="bagNo">袋號。</param>
    /// <returns>尚未掃描的分提單號與備註。</returns>
    private async Task<List<UnpackingUploadRow>> GetPendingRowsAsync(string bagNo)
    {
        var rows = await _jetfDbContext.B6fUnpackingUploads
            .AsNoTracking()
            .Where(entity => entity.BagNo == bagNo && entity.ScanUploadOpe2 == string.Empty)
            .Select(entity => new UnpackingUploadRow
            {
                TrackingNo = entity.TrackingNo,
                Remark = entity.Remark
            })
            .ToListAsync();

        return rows;
    }

    /// <summary>
    /// 將拆袋資料轉為分提單號清單，去除前後空白並排除空值。
    /// </summary>
    /// <param name="rows">拆袋資料。</param>
    /// <returns>分提單號清單。</returns>
    private static IReadOnlyList<string> ToTrackingNos(IEnumerable<UnpackingUploadRow> rows)
    {
        return rows
            .Select(row => row.TrackingNo?.Trim() ?? string.Empty)
            .Where(trackingNo => trackingNo.Length > 0)
            .ToList();
    }

    /// <summary>
    /// 取得上傳時間（取到秒，與舊 PDT API 一致）。
    /// </summary>
    /// <returns>目前時間（去除毫秒）。</returns>
    private static DateTime GetUploadTime()
    {
        var now = DateTime.Now;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
    }

    /// <summary>
    /// 新增 API 紀錄（PdtUnpacking）。
    /// </summary>
    /// <param name="api">API 名稱（BagNo、TrackingNo）。</param>
    /// <param name="dataType">作業地區。</param>
    /// <param name="bagNo">袋號。</param>
    /// <param name="trackingNo">分提單號；袋號掃描時為 null。</param>
    /// <param name="uploadOpe">上傳人員。</param>
    /// <param name="uploadTime">上傳時間。</param>
    private async Task InsertPdtUnpackingAsync(
        string api,
        string dataType,
        string bagNo,
        string? trackingNo,
        string uploadOpe,
        DateTime uploadTime)
    {
        await _jetfDbContext.PdtUnpackings.AddAsync(new PdtUnpackingEntity
        {
            Api = api,
            DataType = dataType,
            BagNo = bagNo,
            TrackingNo = trackingNo,
            UploadOpe = uploadOpe,
            UploadTime = uploadTime
        });
        await _jetfDbContext.SaveChangesAsync();

        // 只寫入紀錄不需追蹤，避免同一個 DbContext 再次新增相同邏輯主鍵時衝突
        _jetfDbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// 發送 Telegram 訊息至拆袋通知群組，Bot Token 取自 TelegramBotConfig。
    /// 失敗時僅記錄日誌，不影響拆袋作業。
    /// </summary>
    /// <param name="message">訊息內容。</param>
    private async Task SendTelegramAsync(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_unpackingOptions.TelegramChatId))
            {
                _logger.LogWarning("未設定拆袋 Telegram ChatId，略過發送");
                return;
            }

            var botToken = (await _jetfDbContext.TelegramBotConfigs
                .AsNoTracking()
                .Select(entity => entity.BotToken)
                .FirstOrDefaultAsync())?.Trim();
            if (string.IsNullOrWhiteSpace(botToken))
            {
                _logger.LogWarning("找不到 Telegram Bot Token，略過發送");
                return;
            }

            var client = _httpClientFactory.CreateClient(TelegramHttpClientName);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = _unpackingOptions.TelegramChatId,
                ["text"] = message
            });
            using var response = await client.PostAsync($"https://api.telegram.org/bot{botToken}/sendMessage", content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "拆袋 Telegram 發送失敗，StatusCode: {StatusCode}, Body: {Body}",
                    (int)response.StatusCode,
                    await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "拆袋 Telegram 發送發生錯誤");
        }
    }
}
