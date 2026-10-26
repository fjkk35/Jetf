using System.Security.Claims;
using TaxPortalApi.Models.Auth;

namespace TaxPortalApi.Services.Interfaces;

public interface IAuthService
{
    Task<string> LoginAsync(LoginRequest request);

    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal);
}