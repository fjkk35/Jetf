using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxPortalApi.Models.Auth;
using TaxPortalApi.Models.Common;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Controllers;

/// <summary>
/// 權限
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// 登入
    /// </summary>
    /// <param name="request">登入請求資料。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>包含 JWT Token 的統一回應。</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<string>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await authService.LoginAsync(request, cancellationToken);
        return ApiResponse<string>.Ok(token, "登入成功");
    }

    /// <summary>
    /// 取得目前登入使用者
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>目前登入使用者資訊。</returns>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        return ApiResponse<CurrentUserResponse>.Ok(currentUser);
    }
}