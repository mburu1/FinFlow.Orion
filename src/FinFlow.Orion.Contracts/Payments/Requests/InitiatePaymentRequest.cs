namespace FinFlow.Orion.Contracts.Payments.Requests;

public sealed class InitiatePaymentRequest
{
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "KES";
    public string Provider { get; init; } = string.Empty;       // MPesa, Card, BankTransfer
    public string Channel { get; init; } = string.Empty;        // Mobile, Web, Api
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Description { get; init; }

    // Required only when Provider == "BankTransfer".
    public string? BankAccountNumber { get; init; }
    public string? BankCode { get; init; }
    public string? BankAccountName { get; init; }

    public Dictionary<string, string> Metadata { get; init; } = [];
}