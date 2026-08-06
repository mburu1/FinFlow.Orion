namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IIdempotencyService
{
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task StoreAsync(string key, string response, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
}