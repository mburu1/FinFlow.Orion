using FinFlow.Orion.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FinFlow.Orion.Infrastructure.Persistence;

namespace FinFlow.Orion.Infrastructure.Idempotency;

/// <summary>
/// SQL Server-backed idempotency store.
/// Persists idempotency keys alongside their cached responses
/// directly in the same transaction as the payment write.
/// This guarantees that a key is never stored without its corresponding
/// payment row — no distributed transaction needed.
/// </summary>
public sealed class SqlIdempotencyService : IIdempotencyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SqlIdempotencyService> _logger;

    public SqlIdempotencyService(
        ApplicationDbContext dbContext,
        ILogger<SqlIdempotencyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotencyKeys
            .AnyAsync(k => k.Key == key && k.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    public async Task StoreAsync(
        string key,
        string response,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Key == key, cancellationToken);

        if (existing is not null)
        {
            _logger.LogDebug("[Idempotency] Key {Key} already stored, skipping.", key);
            return;
        }

        var record = IdempotencyRecord.Create(
            key: key,
            response: response,
            expiry: expiry ?? TimeSpan.FromHours(24));

        await _dbContext.IdempotencyKeys.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("[Idempotency] Stored key {Key} — expires: {Expiry}",
            key, record.ExpiresAt);
    }

    public async Task<string?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(
                k => k.Key == key && k.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (record is null)
        {
            _logger.LogDebug("[Idempotency] Key {Key} not found or expired.", key);
            return null;
        }

        return record.Response;
    }
}

// ── Entity ───────────────────────────────────────────────────────────────────

public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = null!;
    public string Response { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private IdempotencyRecord() { } // EF Core

    public static IdempotencyRecord Create(string key, string response, TimeSpan expiry)
    {
        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            Response = response,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiry)
        };
    }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
}