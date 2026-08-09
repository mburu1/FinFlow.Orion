using FinFlow.Orion.Domain.Abstractions;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Ledger;

public sealed class LedgerAccount : AggregateRoot, IAuditableEntity
{
    public string Code { get; private set; } = null!;          // e.g. "1001-MPESA-FLOAT"
    public string Name { get; private set; } = null!;
    public LedgerAccountType AccountType { get; private set; }
    public Money Balance { get; private set; } = null!;
    public string CurrencyCode { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private readonly List<LedgerEntry> _entries = [];
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    private LedgerAccount() { } // EF Core

    public static LedgerAccount Create(
        string code,
        string name,
        LedgerAccountType accountType,
        string currencyCode = "KES")
    {
        return new LedgerAccount
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            AccountType = accountType,
            CurrencyCode = currencyCode,
            Balance = Money.Zero(currencyCode),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Standard double-entry semantics: a debit increases Asset/Expense accounts and
    // decreases Liability/Revenue(/Equity) accounts; a credit does the opposite.
    public void Credit(Money amount)
    {
        Balance = AccountType is LedgerAccountType.Asset or LedgerAccountType.Expense
            ? Balance.Subtract(amount)
            : Balance.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(Money amount)
    {
        Balance = AccountType is LedgerAccountType.Asset or LedgerAccountType.Expense
            ? Balance.Add(amount)
            : Balance.Subtract(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddEntry(LedgerEntry entry) => _entries.Add(entry);
}