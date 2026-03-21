using System.Security.Claims;
using TaxPortalApi.Models.Auth;

namespace TaxPortalApi.Services.Interfaces;

public interface IAuthService
{
    Task<string> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}