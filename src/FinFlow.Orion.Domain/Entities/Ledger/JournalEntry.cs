using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Ledger;

public sealed class JournalEntry : AggregateRoot
{
    public string Description { get; private set; } = null!;
    public string? PaymentReference { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public DateTime PostedAt { get; private set; }
    public string PostedBy { get; private set; } = null!;
    public bool IsBalanced { get; private set; }

    private readonly List<LedgerEntry> _entries = [];
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    private JournalEntry() { } // EF Core

    public static JournalEntry Create(
        string description,
        Money totalAmount,
        string postedBy,
        string? paymentReference = null)
    {
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            Description = description,
            TotalAmount = totalAmount,
            PostedBy = postedBy,
            PaymentReference = paymentReference,
            PostedAt = DateTime.UtcNow,
            IsBalanced = false
        };
    }

    public void AddEntry(LedgerEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Double-entry invariant: sum of debits must equal sum of credits.
    /// </summary>
    public void Validate()
    {
        var credits = _entries
            .Where(e => e.IsCredit)
            .Sum(e => e.Amount.Amount);

        var debits = _entries
            .Where(e => e.IsDebit)
            .Sum(e => e.Amount.Amount);

        IsBalanced = credits == debits;

        if (!IsBalanced)
            throw new InvalidOperationException(
                $"Journal entry is unbalanced. Credits: {credits}, Debits: {debits}");
    }
}