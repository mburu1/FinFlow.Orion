using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Infrastructure.Providers.Bank;

public interface IBankProvider
{
    Task<BankTransferResult> InitiateTransferAsync(
        BankTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<BankTransferResult> QueryTransferStatusAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default);

    Task<BankTransferResult> ReverseTransferAsync(
        string providerTransactionId,
        Money amount,
        string reason,
        CancellationToken cancellationToken = default);
}

public sealed class BankTransferRequest
{
    public Money Amount { get; init; } = null!;
    public string AccountNumber { get; init; } = string.Empty;
    public string BankCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string Narration { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
}

public sealed class BankTransferResult
{
    public bool IsSuccessful { get; init; }
    public string? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? BankReference { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}