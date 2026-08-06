namespace FinFlow.Orion.Contracts.Payments.Responses;

public sealed class PaymentSummaryResponse
{
    public Guid PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
    public DateTime CreatedAt { get; init; }
}