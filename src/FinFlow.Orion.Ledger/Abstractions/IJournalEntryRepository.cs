using FinFlow.Orion.Domain.Entities.Ledger;

namespace FinFlow.Orion.Ledger.Abstractions;

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<JournalEntry?> GetByPaymentReferenceAsync(
        string paymentReference,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JournalEntry>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        JournalEntry journalEntry,
        CancellationToken cancellationToken = default);
}