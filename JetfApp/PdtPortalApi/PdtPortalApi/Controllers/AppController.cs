using Microsoft.AspNetCore.Mvc;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Services;

namespace PdtPortalApi.Controllers;

/// <summary>
/// App 版本相關 API。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AppController(IAppVersionService appVersionService, ILogger<AppController> logger) : ControllerBase
{
    private readonly IAppVersionService _appVersionService = appVersionService;
    private readonly ILogger<AppController> _logger = logger;

    /// <summary>
    /// 檢查 App 版本是否需要強制更新。
    /// </summary>
    /// <param name="request">版本檢查請求。</param>
    /// <returns>版本檢查結果。</returns>
    [HttpGet("version-check")]
    [ProducesResponseType(typeof(ApiResponse<AppVersionCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppVersionCheckResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppVersionCheckResponse>), StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<AppVersionCheckResponse>> VersionCheck([FromQuery] AppVersionCheckRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.VersionCode))
            {
                return BadRequest(ApiResponse<AppVersionCheckResponse>.Fail("VALIDATION_ERROR", "versionCode 為必填"));
            }

            var result = _appVersionService.GetVersionCheckResult(request.VersionCode);
            return Ok(ApiResponse<AppVersionCheckResponse>.Ok(result));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "App 版本檢查 API 執行失敗，VersionCode: {VersionCode}", request.VersionCode);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<AppVersionCheckResponse>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "App 版本檢查時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }
}