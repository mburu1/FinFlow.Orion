using FinFlow.Orion.Domain.Entities.Identity;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IUserService
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser> RegisterAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<(AppUser User, string AccessToken, RefreshToken RefreshToken)> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<(AppUser User, string AccessToken, RefreshToken RefreshToken)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}