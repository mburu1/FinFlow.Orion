using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Identity;

namespace FinFlow.Orion.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userRepository.FindByEmailAsync(email, cancellationToken);
    }

    public async Task<AppUser> RegisterAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists.");

        var user = AppUser.Create(email, _passwordHasher.HashPassword(password), firstName, lastName);
        await _userRepository.AddAsync(user, cancellationToken);
        return user;
    }

    public async Task<(AppUser User, string AccessToken, RefreshToken RefreshToken)> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (user == null || !_passwordHasher.VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");

        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(user);
        await _userRepository.UpdateAsync(user, cancellationToken); // Save refresh token

        return (user, accessToken, refreshToken);
    }

    public async Task<(AppUser User, string AccessToken, RefreshToken RefreshToken)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _userRepository.GetRefreshTokenAsync(refreshToken, cancellationToken);
        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        // Revoke old token and generate new pair
        user.RevokeRefreshToken(refreshToken);
        var (newAccessToken, newRefreshToken) = _tokenService.GenerateTokens(user);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return (user, newAccessToken, newRefreshToken);
    }
}