using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Ledger;

public sealed class LedgerEntry : Entity
{
    public Guid JournalEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType EntryType { get; private set; }     // Credit or Debit
    public Money Amount { get; private set; } = null!;
    public string Narration { get; private set; } = null!;
    public string? ReferenceId { get; private set; }            // Payment ref / recon ref
    public DateTime PostedAt { get; private set; }

    private LedgerEntry() { } // EF Core

    public static LedgerEntry Create(
        Guid journalEntryId,
        Guid accountId,
        TransactionType entryType,
        Money amount,
        string narration,
        string? referenceId = null)
    {
        return new LedgerEntry
        {
            Id = Guid.NewGuid(),
            JournalEntryId = journalEntryId,
            AccountId = accountId,
            EntryType = entryType,
            Amount = amount,
            Narration = narration,
            ReferenceId = referenceId,
            PostedAt = DateTime.UtcNow
        };
    }

    public bool IsCredit => EntryType == TransactionType.Credit;
    public bool IsDebit => EntryType == TransactionType.Debit;
}