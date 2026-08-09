using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================================
    // FIND BY EMAIL
    // =========================================================================

    public async Task<AppUser?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(
                u => u.Email == normalizedEmail,
                cancellationToken);
    }

    // =========================================================================
    // GET BY ID
    // =========================================================================

    public async Task<AppUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(
                u => u.Id == id,
                cancellationToken);
    }

    // =========================================================================
    // ADD USER
    // =========================================================================

    public async Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================================
    // UPDATE USER
    // =========================================================================

    public async Task UpdateAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        // Only force the whole graph to Modified for a genuinely detached entity.
        // Calling Update() on an already-tracked entity (the normal case — callers
        // load-then-mutate within the same scope) incorrectly flips newly-added
        // child entities (e.g. a fresh RefreshToken) from Added to Modified, which
        // makes EF emit an UPDATE for a row that doesn't exist yet.
        if (_context.Entry(user).State == EntityState.Detached)
            _context.Users.Update(user);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================================
    // GET REFRESH TOKEN
    // =========================================================================

    public async Task<RefreshToken?> GetRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.Token == token,
                cancellationToken);
    }
}