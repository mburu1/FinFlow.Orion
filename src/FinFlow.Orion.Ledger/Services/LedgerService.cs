using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FinFlow.Orion.Ledger.Abstractions;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Ledger.Services;

public sealed class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(
        ILedgerRepository ledgerRepository,
        IJournalEntryRepository journalEntryRepository,
        ILogger<LedgerService> logger)
    {
        _ledgerRepository = ledgerRepository;
        _journalEntryRepository = journalEntryRepository;
        _logger = logger;
    }

    public async Task PostPaymentAsync(
        string paymentReference,
        Money amount,
        string debitAccountCode,
        string creditAccountCode,
        string postedBy,
        CancellationToken cancellationToken = default)
    {
        var (debitAccount, creditAccount) = await ResolveAccountsAsync(
            debitAccountCode, creditAccountCode, cancellationToken);

        // Build journal
        var journal = JournalEntry.Create(
            description: $"Payment received — {paymentReference}",
            totalAmount: amount,
            postedBy: postedBy,
            paymentReference: paymentReference);

        // Double-entry: Debit receivable, Credit float/settlement
        var debitEntry = LedgerEntry.Create(
            journalEntryId: journal.Id,
            accountId: debitAccount.Id,
            entryType: TransactionType.Debit,
            amount: amount,
            narration: $"Payment debit — {paymentReference}",
            referenceId: paymentReference);

        var creditEntry = LedgerEntry.Create(
            journalEntryId: journal.Id,
            accountId: creditAccount.Id,
            entryType: TransactionType.Credit,
            amount: amount,
            narration: $"Payment credit — {paymentReference}",
            referenceId: paymentReference);

        journal.AddEntry(debitEntry);
        journal.AddEntry(creditEntry);
        journal.Validate(); // Throws if credits ≠ debits

        // Apply balances
        debitAccount.Debit(amount);
        creditAccount.Credit(amount);

        debitAccount.AddEntry(debitEntry);
        creditAccount.AddEntry(creditEntry);

        await _journalEntryRepository.AddAsync(journal, cancellationToken);
        await _ledgerRepository.UpdateAsync(debitAccount, cancellationToken);
        await _ledgerRepository.UpdateAsync(creditAccount, cancellationToken);

        _logger.LogInformation(
            "[Ledger] Posted payment journal — Ref: {Ref} | Amount: {Amount} | Debit: {Debit} | Credit: {Credit}",
            paymentReference, amount, debitAccountCode, creditAccountCode);
    }

    public async Task PostReversalAsync(
        string paymentReference,
        Money amount,
        string debitAccountCode,
        string creditAccountCode,
        string postedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var (debitAccount, creditAccount) = await ResolveAccountsAsync(
            debitAccountCode, creditAccountCode, cancellationToken);

        // Reversal flips the original entry — Credit the debit side, Debit the credit side
        var journal = JournalEntry.Create(
            description: $"Reversal — {paymentReference} | Reason: {reason}",
            totalAmount: amount,
            postedBy: postedBy,
            paymentReference: paymentReference);

        var creditReversal = LedgerEntry.Create(
            journalEntryId: journal.Id,
            accountId: debitAccount.Id,
            entryType: TransactionType.Credit,
            amount: amount,
            narration: $"Reversal credit — {paymentReference}",
            referenceId: paymentReference);

        var debitReversal = LedgerEntry.Create(
            journalEntryId: journal.Id,
            accountId: creditAccount.Id,
            entryType: TransactionType.Debit,
            amount: amount,
            narration: $"Reversal debit — {paymentReference}",
            referenceId: paymentReference);

        journal.AddEntry(creditReversal);
        journal.AddEntry(debitReversal);
        journal.Validate();

        // Reverse the balances
        debitAccount.Credit(amount);
        creditAccount.Debit(amount);

        debitAccount.AddEntry(creditReversal);
        creditAccount.AddEntry(debitReversal);

        await _journalEntryRepository.AddAsync(journal, cancellationToken);
        await _ledgerRepository.UpdateAsync(debitAccount, cancellationToken);
        await _ledgerRepository.UpdateAsync(creditAccount, cancellationToken);

        _logger.LogInformation(
            "[Ledger] Posted reversal journal — Ref: {Ref} | Amount: {Amount} | Reason: {Reason}",
            paymentReference, amount, reason);
    }

    public async Task<Money> GetBalanceAsync(
        string accountCode,
        CancellationToken cancellationToken = default)
    {
        var account = await _ledgerRepository.GetByCodeAsync(accountCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Ledger account '{accountCode}' not found.");

        return account.Balance;
    }

    public async Task<bool> ValidateJournalBalanceAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var journal = await _journalEntryRepository.GetByIdAsync(journalEntryId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Journal entry '{journalEntryId}' not found.");

        try
        {
            journal.Validate();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "[Ledger] Journal {Id} failed balance validation: {Reason}",
                journalEntryId, ex.Message);
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task<(LedgerAccount Debit, LedgerAccount Credit)> ResolveAccountsAsync(
        string debitCode,
        string creditCode,
        CancellationToken cancellationToken)
    {
        var debit = await _ledgerRepository.GetByCodeAsync(debitCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Debit ledger account '{debitCode}' not found.");

        var credit = await _ledgerRepository.GetByCodeAsync(creditCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Credit ledger account '{creditCode}' not found.");

        return (debit, credit);
    }
}