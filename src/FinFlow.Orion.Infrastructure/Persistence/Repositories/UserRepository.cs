using FinFlow.Orion.Application.Common.Interfaces; // Ensure this is here
using FinFlow.Orion.Domain.Entities.Identity;
using FinFlow.Orion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }
}