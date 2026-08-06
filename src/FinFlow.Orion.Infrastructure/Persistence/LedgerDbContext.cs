using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence;

/// <summary>
/// Bounded DbContext for the Ledger module.
/// Shares the same SQL Server database as ApplicationDbContext but isolates
/// Ledger entities (LedgerAccounts, LedgerEntries, JournalEntries).
/// This enables future extraction into a separate Ledger microservice.
/// </summary>
public sealed class LedgerDbContext : DbContext
{
    public LedgerDbContext(
        DbContextOptions<LedgerDbContext> options) : base(options)
    {
    }

    // ── Ledger Entities ──────────────────────────────────────────────────────
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply only Ledger-related configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LedgerDbContext).Assembly,
            type => type.Name.Contains("Ledger") || type.Name.Contains("Journal"));
    }

    // Optional: Override SaveChanges to add Ledger-specific audit logic
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add Ledger-specific logic here if needed (e.g., double-entry validation)
        // For now, delegate to base
        return await base.SaveChangesAsync(cancellationToken);
    }
}