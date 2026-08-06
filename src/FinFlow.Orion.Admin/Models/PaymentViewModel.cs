namespace FinFlow.Orion.Admin.Models;

public sealed class PaymentViewModel
{
    public Guid PaymentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}