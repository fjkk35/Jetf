using Microsoft.AspNetCore.Mvc;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Services;

namespace PdtPortalApi.Controllers;

/// <summary>
/// 拆袋作業 API（FTZ、TACT）。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class UnpackingController(
    IUnpackingService unpackingService,
    IHmacSignatureService hmacSignatureService,
    ILogger<UnpackingController> logger) : ControllerBase
{
    private readonly IUnpackingService _unpackingService = unpackingService;
    private readonly IHmacSignatureService _hmacSignatureService = hmacSignatureService;
    private readonly ILogger<UnpackingController> _logger = logger;

    /// <summary>
    /// 掃描袋號，判斷是否需要拆袋。
    /// </summary>
    /// <param name="timestamp">請求時間戳記。</param>
    /// <param name="signature">簽章字串。</param>
    /// <param name="request">袋號掃描請求。</param>
    /// <returns>是否需要拆袋與尚未掃描的分提單號。</returns>
    [HttpPost("bag-no")]
    [ProducesResponseType(typeof(ApiResponse<UnpackingBagResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UnpackingBagResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UnpackingBagResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UnpackingBagResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UnpackingBagResultDto>>> ScanBagNoAsync(
        [FromHeader(Name = "X-Timestamp")] long timestamp,
        [FromHeader(Name = "X-Signature")] string? signature,
        [FromBody] UnpackingBagNoRequest request)
    {
        try
        {
            if (!_hmacSignatureService.IsSignatureValid(request, timestamp, signature))
            {
                return Unauthorized(
                    ApiResponse<UnpackingBagResultDto>.Fail(
                        "INVALID_SIGNATURE",
                        "簽章驗證失敗或 Timestamp 已超過 5 分鐘有效期",
                        StatusCodes.Status401Unauthorized));
            }

            var result = await _unpackingService.ScanBagNoAsync(request);
            var message = result.NeedUnpacking ? "拆袋處理" : "不需拆袋";
            return Ok(ApiResponse<UnpackingBagResultDto>.Ok(result, message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "拆袋袋號掃描 API 執行失敗，BagNo: {BagNo}", request.BagNo);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<UnpackingBagResultDto>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "拆袋袋號掃描時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// 掃描分提單號，判斷是否需拆出。
    /// </summary>
    /// <param name="timestamp">請求時間戳記。</param>
    /// <param name="signature">簽章字串。</param>
    /// <param name="request">分提單號掃描請求。</param>
    /// <returns>是否需拆出與拆出提示訊息。</returns>
    [HttpPost("tracking-no")]
    [ProducesResponseType(typeof(ApiResponse<UnpackingTrackingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UnpackingTrackingResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UnpackingTrackingResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UnpackingTrackingResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UnpackingTrackingResultDto>>> ScanTrackingNoAsync(
        [FromHeader(Name = "X-Timestamp")] long timestamp,
        [FromHeader(Name = "X-Signature")] string? signature,
        [FromBody] UnpackingTrackingNoRequest request)
    {
        try
        {
            if (!_hmacSignatureService.IsSignatureValid(request, timestamp, signature))
            {
                return Unauthorized(
                    ApiResponse<UnpackingTrackingResultDto>.Fail(
                        "INVALID_SIGNATURE",
                        "簽章驗證失敗或 Timestamp 已超過 5 分鐘有效期",
                        StatusCodes.Status401Unauthorized));
            }

            var result = await _unpackingService.ScanTrackingNoAsync(request);
            var message = result.IsUnpackTarget ? result.ResultMessage : "非此袋拆出貨件";
            return Ok(ApiResponse<UnpackingTrackingResultDto>.Ok(result, message));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "拆袋分提單號掃描 API 執行失敗，BagNo: {BagNo}, TrackingNo: {TrackingNo}",
                request.BagNo,
                request.TrackingNo);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<UnpackingTrackingResultDto>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "拆袋分提單號掃描時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// 查詢袋號尚未掃描的分提單號。
    /// </summary>
    /// <param name="request">查詢請求。</param>
    /// <returns>尚未掃描的分提單號；全部掃描完成時為空陣列。</returns>
    [HttpPost("pending")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetPendingAsync(
        [FromBody] UnpackingPendingRequest request)
    {
        try
        {
            var result = await _unpackingService.GetPendingTrackingNosAsync(request.BagNo);
            var message = result.Count == 0 ? "已全部掃描完成" : $"尚有 {result.Count} 筆分提單號未掃描";
            return Ok(ApiResponse<IReadOnlyList<string>>.Ok(result, message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "拆袋未掃描分提單號查詢 API 執行失敗，BagNo: {BagNo}", request.BagNo);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<IReadOnlyList<string>>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "查詢未掃描分提單號時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }
}
