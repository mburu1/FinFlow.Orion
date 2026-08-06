using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Infrastructure.Providers.MPesa;

public interface IMpesaProvider
{
    /// <summary>
    /// Initiates an STK Push (Lipa Na M-Pesa Online) request.
    /// </summary>
    Task<MpesaPaymentResult> InitiateStkPushAsync(
        PhoneNumber phoneNumber,
        Money amount,
        string accountReference,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the status of a pending STK Push transaction.
    /// </summary>
    Task<MpesaPaymentResult> QueryTransactionStatusAsync(
        string checkoutRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a reversal for a completed M-Pesa transaction.
    /// </summary>
    Task<MpesaReversalResult> ReverseTransactionAsync(
        string transactionId,
        Money amount,
        string remarks,
        CancellationToken cancellationToken = default);
}

public sealed class MpesaPaymentResult
{
    public bool IsSuccessful { get; init; }
    public string? CheckoutRequestId { get; init; }
    public string? MerchantRequestId { get; init; }
    public string? TransactionId { get; init; }
    public string? ResponseCode { get; init; }
    public string? ResponseDescription { get; init; }
    public string? CustomerMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed class MpesaReversalResult
{
    public bool IsSuccessful { get; init; }
    public string? ConversationId { get; init; }
    public string? ResponseCode { get; init; }
    public string? ResponseDescription { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}