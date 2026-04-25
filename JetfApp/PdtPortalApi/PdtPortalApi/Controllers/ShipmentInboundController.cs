using Microsoft.AspNetCore.Mvc;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Services;

namespace PdtPortalApi.Controllers;

/// <summary>
/// 入庫相關 API。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ShipmentInboundController(
    IPortalService portalService,
    IHmacSignatureService hmacSignatureService,
    ILogger<ShipmentInboundController> logger) : ControllerBase
{
    private readonly IPortalService _portalService = portalService;
    private readonly IHmacSignatureService _hmacSignatureService = hmacSignatureService;
    private readonly ILogger<ShipmentInboundController> _logger = logger;

    /// <summary>
    /// 取得貨件來源清單。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>貨件來源清單。</returns>
    [HttpGet("source-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentInboundSourceTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentInboundSourceTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShipmentInboundSourceTypeDto>>>> GetSourceTypesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var data = await _portalService.GetShipmentInboundSourcesAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<ShipmentInboundSourceTypeDto>>.Ok(data));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "查詢貨件來源 API 執行失敗");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<IReadOnlyList<ShipmentInboundSourceTypeDto>>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "查詢貨件來源時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// 檢查是否存在原始入庫資料。
    /// </summary>
    /// <param name="request">檢查請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>是否存在原始入庫資料。</returns>
    [HttpPost("check")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> CheckAsync([FromBody] TrackingNoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _portalService.CheckInboundDataAsync(request.TrackingNo, request.SourceType, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(result.IsValid, result.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "檢查原始入庫資料 API 執行失敗，TrackingNo: {TrackingNo}, SourceType: {SourceType}",
                request.TrackingNo,
                request.SourceType);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<bool>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "檢查原始入庫資料時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// 寫入入庫資料。
    /// </summary>
    /// <param name="timestamp">請求時間戳記。</param>
    /// <param name="signature">簽章字串。</param>
    /// <param name="request">入庫請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>寫入結果。</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> CreateAsync(
        [FromHeader(Name = "X-Timestamp")] long timestamp,
        [FromHeader(Name = "X-Signature")] string? signature,
        [FromBody] CreateShipmentInboundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_hmacSignatureService.IsSignatureValid(request, timestamp, signature))
            {
                return Unauthorized(
                    ApiResponse<bool>.Fail(
                        "INVALID_SIGNATURE",
                        "簽章驗證失敗或 Timestamp 已超過 5 分鐘有效期",
                        StatusCodes.Status401Unauthorized));
            }

            var result = await _portalService.CreateShipmentInboundAsync(request, cancellationToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.Code, ApiResponse<bool>.Fail(result.ErrorCode, result.Message, result.Code));
            }

            return Ok(ApiResponse<bool>.Ok(true, result.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "寫入入庫資料 API 執行失敗，TrackingNo: {TrackingNo}", request.TrackingNo);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<bool>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "寫入入庫資料時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// 寫入異常件資料。
    /// </summary>
    /// <param name="request">異常件請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>寫入結果。</returns>
    [HttpPost("exception")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> CreateExceptionAsync(
        [FromHeader(Name = "X-Timestamp")] long timestamp,
        [FromHeader(Name = "X-Signature")] string? signature,
        [FromBody] CreateShipmentInboundExceptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_hmacSignatureService.IsSignatureValid(request, timestamp, signature))
            {
                return Unauthorized(
                    ApiResponse<bool>.Fail(
                        "INVALID_SIGNATURE",
                        "簽章驗證失敗或 Timestamp 已超過 5 分鐘有效期",
                        StatusCodes.Status401Unauthorized));
            }

            var result = await _portalService.CreateShipmentInboundExceptionAsync(request, cancellationToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.Code, ApiResponse<bool>.Fail(result.ErrorCode, result.Message, result.Code));
            }

            return Ok(ApiResponse<bool>.Ok(true, result.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "寫入異常件資料 API 執行失敗，SeqNo: {SeqNo}", request.SeqNo);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<bool>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "寫入異常件資料時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }
}
