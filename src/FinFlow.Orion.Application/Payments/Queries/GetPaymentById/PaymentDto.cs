namespace FinFlow.Orion.Application.Payments.Queries.GetPaymentById;

public sealed class PaymentDto
{
    public Guid PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Description { get; init; }
    public string? ProviderTransactionId { get; init; }
    public string? FailureReason { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}