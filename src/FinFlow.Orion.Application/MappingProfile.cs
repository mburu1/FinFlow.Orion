using FinFlow.Orion.Contracts.Payments.Requests;
using FinFlow.Orion.Contracts.Reconciliation.Requests;
using FinFlow.Orion.Contracts.Webhooks.Requests;
using FinFlow.Orion.Application.Payments.Commands.InitiatePayment;
using FinFlow.Orion.Application.Payments.Commands.RetryPayment;
using FinFlow.Orion.Application.Payments.Commands.ReversePayment;
using FinFlow.Orion.Application.Reconciliation.Commands.TriggerReconciliation;
using FinFlow.Orion.Application.Reconciliation.Commands.ResolveDiscrepancy;
using FinFlow.Orion.Application.Webhooks.Commands.ReplayWebhook;

namespace FinFlow.Orion.Application;

/// <summary>
/// Manual mapping helpers from Contract requests to Application commands.
/// No AutoMapper dependency — keeps the Application layer lightweight.
/// </summary>
public static class MappingProfile
{
    // Payments
    public static InitiatePaymentCommand ToCommand(this InitiatePaymentRequest request) =>
        new(request.Amount,
            request.CurrencyCode,
            request.Provider,
            request.Channel,
            request.IdempotencyKey,
            request.CustomerId,
            request.PhoneNumber,
            request.Description,
            request.BankAccountNumber,
            request.BankCode,
            request.BankAccountName,
            request.Metadata);

    public static RetryPaymentCommand ToCommand(this RetryPaymentRequest request) =>
        new(request.PaymentId,
            request.IdempotencyKey,
            request.OverrideProvider);

    public static ReversePaymentCommand ToCommand(this ReversePaymentRequest request, string requestedBy) =>
        new(request.PaymentId,
            request.Reason,
            requestedBy,
            request.IdempotencyKey);

    // Reconciliation
    public static TriggerReconciliationCommand ToCommand(
        this TriggerReconciliationRequest request) =>
        new(request.Provider,
            request.ReconDate,
            request.TriggeredBy);

    public static ResolveDiscrepancyCommand ToCommand(
        this ResolveDiscrepancyRequest request, string resolvedBy) =>
        new(request.DiscrepancyId,
            resolvedBy,
            request.Notes);

    // Webhooks
    public static ReplayWebhookCommand ToCommand(
        this ReplayWebhookRequest request) =>
        new(request.WebhookEventId,
            request.ReplayedBy,
            request.Reason);
}