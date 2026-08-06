namespace FinFlow.Orion.Contracts.Payments.Requests;

public sealed class ReversePaymentRequest
{
    public Guid PaymentId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
}