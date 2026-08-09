using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Ledger.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories
{
    public class LedgerRepository : ILedgerRepository
    {
        private readonly LedgerDbContext _context;

        public LedgerRepository(LedgerDbContext context)
        {
            _context = context;
        }

        // ── LedgerAccount Methods ─────────────────────────────────────────────

        // ✅ Return type fixed: Task<LedgerAccount?> (nullable)
        public async Task<LedgerAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.LedgerAccounts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        // ✅ Return type fixed: Task<LedgerAccount?> (nullable)
        public async Task<LedgerAccount?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return await _context.LedgerAccounts
                .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        }

        public async Task<IReadOnlyList<LedgerAccount>> GetAllActiveAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.LedgerAccounts
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            LedgerAccount account,
            CancellationToken cancellationToken = default)
        {
            await _context.LedgerAccounts.AddAsync(account, cancellationToken);
        }

        public async Task UpdateAsync(
            LedgerAccount account,
            CancellationToken cancellationToken = default)
        {
            // Only force the whole graph to Modified for a genuinely detached
            // entity — LedgerService loads accounts via GetByCodeAsync (already
            // tracked) and its LedgerEntry children are typically already saved
            // via JournalEntryRepository.AddAsync by the time this runs, so an
            // unconditional Update() risks the same Added→Modified graph-tracking
            // hazard fixed in UserRepository.UpdateAsync.
            if (_context.Entry(account).State == EntityState.Detached)
                _context.LedgerAccounts.Update(account);

            await _context.SaveChangesAsync(cancellationToken);
        }

        // ── LedgerEntry Methods ───────────────────────────────────────────────

        public async Task<IReadOnlyList<LedgerEntry>> GetEntriesByAccountAsync(
            Guid accountId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);

            return await _context.LedgerEntries
                .Where(e => e.AccountId == accountId &&
                            e.PostedAt >= start &&
                            e.PostedAt <= end)
                .OrderByDescending(e => e.PostedAt)
                .ToListAsync(cancellationToken);
        }
    }
}