using FinFlow.Orion.Domain.Entities.Ledger;

namespace FinFlow.Orion.Ledger.Abstractions;

public interface ILedgerRepository
{
    Task<LedgerAccount?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LedgerAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerAccount>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        LedgerAccount account,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        LedgerAccount account,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerEntry>> GetEntriesByAccountAsync(
        Guid accountId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}