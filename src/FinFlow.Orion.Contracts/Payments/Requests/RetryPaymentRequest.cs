namespace FinFlow.Orion.Contracts.Payments.Requests;

public sealed class RetryPaymentRequest
{
    public Guid PaymentId { get; init; }
    public string? OverrideProvider { get; init; }              // Optional provider fallback
    public string IdempotencyKey { get; init; } = string.Empty;
}