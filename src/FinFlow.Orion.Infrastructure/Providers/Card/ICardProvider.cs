using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Infrastructure.Providers.Card;

public interface ICardProvider
{
    Task<CardPaymentResult> AuthorizeAsync(
        CardPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<CardPaymentResult> CaptureAsync(
        string providerTransactionId,
        Money amount,
        CancellationToken cancellationToken = default);

    Task<CardPaymentResult> RefundAsync(
        string providerTransactionId,
        Money amount,
        string reason,
        CancellationToken cancellationToken = default);
}

public sealed class CardPaymentRequest
{
    public Money Amount { get; init; } = null!;
    public string Currency { get; init; } = "KES";
    public string CustomerId { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = [];
}

public sealed class CardPaymentResult
{
    public bool IsSuccessful { get; init; }
    public string? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}