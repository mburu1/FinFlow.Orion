using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Ledger.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public sealed class JournalEntryRepository : IJournalEntryRepository
{
    private readonly LedgerDbContext _context;

    public JournalEntryRepository(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<JournalEntry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(j => j.Entries)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<JournalEntry?> GetByPaymentReferenceAsync(
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(j => j.Entries)
            .FirstOrDefaultAsync(j => j.PaymentReference == paymentReference, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDate = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await _context.JournalEntries
            .Include(j => j.Entries)
            .AsNoTracking()
            .Where(j => j.PostedAt >= fromDate && j.PostedAt <= toDate)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        JournalEntry journalEntry,
        CancellationToken cancellationToken = default)
    {
        await _context.JournalEntries.AddAsync(journalEntry, cancellationToken);
    }
}