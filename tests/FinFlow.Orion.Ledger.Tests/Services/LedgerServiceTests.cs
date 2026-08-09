using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FinFlow.Orion.Ledger.Abstractions;
using FinFlow.Orion.Ledger.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Ledger.Tests.Services;

public class LedgerServiceTests
{
    private readonly ILedgerRepository _ledgerRepository = Substitute.For<ILedgerRepository>();
    private readonly IJournalEntryRepository _journalEntryRepository = Substitute.For<IJournalEntryRepository>();
    private readonly ILogger<LedgerService> _logger = Substitute.For<ILogger<LedgerService>>();

    private LedgerService CreateService() => new(_ledgerRepository, _journalEntryRepository, _logger);

    private static LedgerAccount CreateAccount(string code, LedgerAccountType type)
        => LedgerAccount.Create(code, $"{code} account", type, "KES");

    [Fact]
    public async Task PostPaymentAsync_BuildsBalancedJournal_AndAppliesBalancesToBothAccounts()
    {
        // Fresh zero-balance accounts — standard double-entry semantics mean debiting
        // an Asset account and crediting a Liability account both increase the balance.
        var debitAccount = CreateAccount("1002-CARD-SETTLE", LedgerAccountType.Asset);
        var creditAccount = CreateAccount("2001-CUSTOMER-PAYABLE", LedgerAccountType.Liability);

        _ledgerRepository.GetByCodeAsync("1002-CARD-SETTLE", Arg.Any<CancellationToken>()).Returns(debitAccount);
        _ledgerRepository.GetByCodeAsync("2001-CUSTOMER-PAYABLE", Arg.Any<CancellationToken>()).Returns(creditAccount);

        var service = CreateService();
        await service.PostPaymentAsync(
            "REF-123", new Money(500, "KES"), "1002-CARD-SETTLE", "2001-CUSTOMER-PAYABLE", "tester");

        debitAccount.Balance.Amount.Should().Be(500);
        creditAccount.Balance.Amount.Should().Be(500);

        await _journalEntryRepository.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j => j.IsBalanced && j.PaymentReference == "REF-123"),
            Arg.Any<CancellationToken>());
        await _ledgerRepository.Received(1).UpdateAsync(debitAccount, Arg.Any<CancellationToken>());
        await _ledgerRepository.Received(1).UpdateAsync(creditAccount, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostPaymentAsync_UnknownDebitAccount_ThrowsInvalidOperationException()
    {
        _ledgerRepository.GetByCodeAsync("UNKNOWN", Arg.Any<CancellationToken>()).Returns((LedgerAccount?)null);

        var service = CreateService();
        var act = () => service.PostPaymentAsync(
            "REF-1", new Money(100, "KES"), "UNKNOWN", "2001-CUSTOMER-PAYABLE", "tester");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PostReversalAsync_FlipsTheOriginalAccountPair()
    {
        var debitAccount = CreateAccount("1002-CARD-SETTLE", LedgerAccountType.Asset);
        var creditAccount = CreateAccount("2001-CUSTOMER-PAYABLE", LedgerAccountType.Liability);

        // Simulate the original payment posting first (Debit the Asset settlement
        // account, Credit the Liability payable account), so there's a real balance
        // for the reversal to unwind.
        debitAccount.Debit(new Money(500, "KES"));
        creditAccount.Credit(new Money(500, "KES"));

        _ledgerRepository.GetByCodeAsync("1002-CARD-SETTLE", Arg.Any<CancellationToken>()).Returns(debitAccount);
        _ledgerRepository.GetByCodeAsync("2001-CUSTOMER-PAYABLE", Arg.Any<CancellationToken>()).Returns(creditAccount);

        var service = CreateService();
        await service.PostReversalAsync(
            "REF-123", new Money(500, "KES"), "1002-CARD-SETTLE", "2001-CUSTOMER-PAYABLE", "tester", "customer refund");

        // Reversal flips the pair — Credit on the Asset account and Debit on the
        // Liability account — unwinding the original posting back to zero.
        debitAccount.Balance.Amount.Should().Be(0);
        creditAccount.Balance.Amount.Should().Be(0);
    }

    [Fact]
    public async Task GetBalanceAsync_UnknownAccountCode_ThrowsInvalidOperationException()
    {
        _ledgerRepository.GetByCodeAsync("UNKNOWN", Arg.Any<CancellationToken>()).Returns((LedgerAccount?)null);

        var service = CreateService();
        var act = () => service.GetBalanceAsync("UNKNOWN");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetBalanceAsync_KnownAccount_ReturnsBalance()
    {
        var account = CreateAccount("1002-CARD-SETTLE", LedgerAccountType.Asset);
        account.Debit(new Money(250, "KES"));
        _ledgerRepository.GetByCodeAsync("1002-CARD-SETTLE", Arg.Any<CancellationToken>()).Returns(account);

        var service = CreateService();
        var balance = await service.GetBalanceAsync("1002-CARD-SETTLE");

        balance.Amount.Should().Be(250);
    }

    [Fact]
    public async Task ValidateJournalBalanceAsync_BalancedJournal_ReturnsTrue()
    {
        var journal = JournalEntry.Create("desc", new Money(100, "KES"), "tester");
        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Debit, new Money(100, "KES"), "d"));
        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Credit, new Money(100, "KES"), "c"));

        _journalEntryRepository.GetByIdAsync(journal.Id, Arg.Any<CancellationToken>()).Returns(journal);

        var result = await CreateService().ValidateJournalBalanceAsync(journal.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateJournalBalanceAsync_UnbalancedJournal_ReturnsFalse()
    {
        var journal = JournalEntry.Create("desc", new Money(100, "KES"), "tester");
        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Debit, new Money(100, "KES"), "d"));
        journal.AddEntry(LedgerEntry.Create(journal.Id, Guid.NewGuid(), TransactionType.Credit, new Money(40, "KES"), "c"));

        _journalEntryRepository.GetByIdAsync(journal.Id, Arg.Any<CancellationToken>()).Returns(journal);

        var result = await CreateService().ValidateJournalBalanceAsync(journal.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateJournalBalanceAsync_UnknownJournal_ThrowsInvalidOperationException()
    {
        _journalEntryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((JournalEntry?)null);

        var act = () => CreateService().ValidateJournalBalanceAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
