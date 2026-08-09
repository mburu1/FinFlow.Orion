using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Identity;
using FinFlow.Orion.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinFlow.Orion.Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly JwtConfiguration _config;

    public JwtTokenService(IOptions<JwtConfiguration> config)
    {
        _config = config.Value;
    }

    public string GenerateAccessToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public (string AccessToken, RefreshToken RefreshToken) GenerateTokens(AppUser user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = new RefreshToken(
            user.Id,
            GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(_config.RefreshTokenExpiryDays)
        );

        user.AddRefreshToken(refreshToken);
        return (accessToken, refreshToken);
    }
}