using Microsoft.AspNetCore.Mvc;
using PdtPortalApi.Models.Requests;
using PdtPortalApi.Models.Responses;
using PdtPortalApi.Services;

namespace PdtPortalApi.Controllers;

/// <summary>
/// 登入相關 API。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IPortalService portalService, IAppVersionService appVersionService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IPortalService _portalService = portalService;
    private readonly IAppVersionService _appVersionService = appVersionService;
    private readonly ILogger<AuthController> _logger = logger;

    /// <summary>
    /// 使用帳號檢查使用者是否存在。
    /// </summary>
    /// <param name="request">登入請求。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>標準化登入結果。</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status426UpgradeRequired)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_appVersionService.IsVersionAllowed(request.VersionCode))
            {
                return StatusCode(
                    StatusCodes.Status426UpgradeRequired,
                    ApiResponse<bool>.Fail(
                        "APP_VERSION_EXPIRED",
                        "目前版本過舊，請更新後再使用",
                        StatusCodes.Status426UpgradeRequired));
            }

            var isSuccess = await _portalService.LoginAsync(request.Account, cancellationToken);
            var message = isSuccess ? "登入成功" : "查無此帳號";
            return Ok(ApiResponse<bool>.Ok(isSuccess, message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "登入 API 執行失敗，Account: {Account}", request.Account);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<bool>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "登入處理時發生未預期錯誤",
                    StatusCodes.Status500InternalServerError));
        }
    }
}