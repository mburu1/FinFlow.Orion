namespace FinFlow.Orion.Application.Payments.Queries.GetPaymentsByCustomer;

public sealed class PaymentSummaryDto
{
    public Guid PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}