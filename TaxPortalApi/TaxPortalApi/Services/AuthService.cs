using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaxPortalApi.Infrastructure.Exceptions;
using TaxPortalApi.Infrastructure.Persistence;
using TaxPortalApi.Models.Auth;
using TaxPortalApi.Services.Interfaces;

namespace TaxPortalApi.Services;

public class AuthService(JetfDbContext dbContext, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<string> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        var user = await dbContext.TaxPortalUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserName == userName, cancellationToken);

        if (user is null)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "帳號或密碼錯誤", "AUTH_001");
        }

        var passwordMatched = false;

        try
        {
            passwordMatched = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            passwordMatched = false;
        }

        if (!passwordMatched)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "帳號或密碼錯誤", "AUTH_001");
        }

        return jwtTokenService.CreateToken(user.Id, user.UserName);
    }

    public Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userName = principal.Identity?.Name;
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(rawUserId) || !long.TryParse(rawUserId, out var userId))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "無法識別目前使用者", "AUTH_002");
        }

        return Task.FromResult(new CurrentUserResponse
        {
            UserId = userId,
            UserName = userName
        });
    }
}