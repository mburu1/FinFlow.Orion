using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Entities.Identity;

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { } // EF Core

    public RefreshToken(Guid userId, string token, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
        ExpiresAt = DateTime.UtcNow; // Shorten expiry to immediate
    }
}