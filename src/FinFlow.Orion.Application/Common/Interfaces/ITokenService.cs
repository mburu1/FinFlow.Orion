using FinFlow.Orion.Domain.Entities.Identity;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    (string AccessToken, RefreshToken RefreshToken) GenerateTokens(AppUser user);
}