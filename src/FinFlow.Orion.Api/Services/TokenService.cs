using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinFlow.Orion.Api.Services;

public interface ITokenService
{
    string GenerateToken(
        string userId,
        string email,
        IEnumerable<string>? roles = null);
}

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
        => _configuration = configuration;

    public string GenerateToken(
        string userId,
        string email,
        IEnumerable<string>? roles = null)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");

        var expiry = int.Parse(
            _configuration["Jwt:ExpiryMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),

            new(JwtRegisteredClaimNames.Email, email),

            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, role));
            }
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}