using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Ledger.Abstractions;

public interface ILedgerService
{
    /// <summary>
    /// Posts a double-entry journal for a completed payment.
    /// Debits the receivable account, credits the float/settlement account.
    /// </summary>
    Task PostPaymentAsync(
        string paymentReference,
        Money amount,
        string debitAccountCode,
        string creditAccountCode,
        string postedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a reversal journal entry — mirror of the original posting.
    /// </summary>
    Task PostReversalAsync(
        string paymentReference,
        Money amount,
        string debitAccountCode,
        string creditAccountCode,
        string postedBy,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current balance of a ledger account by its code.
    /// </summary>
    Task<Money> GetBalanceAsync(
        string accountCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that debits equal credits for a given journal entry.
    /// </summary>
    Task<bool> ValidateJournalBalanceAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default);
}