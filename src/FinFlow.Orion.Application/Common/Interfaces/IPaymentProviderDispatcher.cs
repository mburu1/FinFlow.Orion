using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Application.Common.Interfaces;

/// <summary>
/// Routes a payment to its configured provider (M-Pesa/Card/BankTransfer) and reports
/// back how far it got. Implemented in Infrastructure, where the concrete provider
/// clients live — Application only depends on this abstraction.
/// </summary>
public interface IPaymentProviderDispatcher
{
    Task<ProviderDispatchOutcome> DispatchAsync(
        Payment payment,
        BankTransferDetails? bankTransferDetails = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Destination account details required to route a payment over BankTransfer.
/// Not persisted on the Payment aggregate — only needed at dispatch time.
/// </summary>
public sealed record BankTransferDetails(string AccountNumber, string BankCode, string AccountName);

/// <summary>
/// Result of routing a payment to its provider.
/// - Card succeeds synchronously: IsAuthorized and IsCaptured are both true.
/// - M-Pesa/BankTransfer only submit: IsAuthorized is true, IsCaptured is false —
///   completion arrives later via the provider's webhook callback.
/// - On failure, IsAuthorized is false and FailureReason explains why.
/// </summary>
public sealed record ProviderDispatchOutcome(
    bool IsAuthorized,
    bool IsCaptured,
    ProviderResponse Response,
    string? FailureReason = null);
