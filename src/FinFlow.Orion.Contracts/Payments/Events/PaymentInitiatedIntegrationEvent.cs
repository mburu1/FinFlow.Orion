namespace FinFlow.Orion.Contracts.Payments.Events;

public sealed class PaymentInitiatedIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}