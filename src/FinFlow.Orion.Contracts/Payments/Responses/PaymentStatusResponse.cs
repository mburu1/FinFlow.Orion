namespace FinFlow.Orion.Contracts.Payments.Responses;

public sealed class PaymentStatusResponse
{
    public Guid PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string? ProviderTransactionId { get; init; }
    public string? FailureReason { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}