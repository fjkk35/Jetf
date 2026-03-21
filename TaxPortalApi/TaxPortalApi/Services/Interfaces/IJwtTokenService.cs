namespace TaxPortalApi.Services.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(long userId, string userName);
}