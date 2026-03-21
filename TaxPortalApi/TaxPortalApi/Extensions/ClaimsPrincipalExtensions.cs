using System.Security.Claims;
using TaxPortalApi.Infrastructure.Exceptions;

namespace TaxPortalApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(rawUserId) || !long.TryParse(rawUserId, out var userId))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "無法識別目前使用者", "AUTH_002");
        }

        return userId;
    }
}