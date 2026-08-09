using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.Entities.Ledger;

public class JournalEntryTests
{
    [Fact]
    public void Validate_BalancedEntries_DoesNotThrow_AndSetsIsBalanced()
    {
        var journal = JournalEntry.Create("Test journal", new Money(100, "KES"), "tester");

        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Debit, new Money(100, "KES"), "debit"));
        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Credit, new Money(100, "KES"), "credit"));

        var act = () => journal.Validate();

        act.Should().NotThrow();
        journal.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnbalancedEntries_Throws()
    {
        var journal = JournalEntry.Create("Test journal", new Money(100, "KES"), "tester");

        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Debit, new Money(100, "KES"), "debit"));
        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Credit, new Money(50, "KES"), "credit"));

        var act = () => journal.Validate();

        act.Should().Throw<InvalidOperationException>();
    }
}
