using FinFlow.Orion.Contracts.Common;
using FinFlow.Orion.Contracts.Payments.Responses;
using FinFlow.Orion.Contracts.Reconciliation.Responses;
using FinFlow.Orion.Contracts.Webhooks.Responses;

namespace FinFlow.Orion.Admin.Services;

public interface IPaymentService
{
    // ── Payments ──────────────────────────────────────────────────────────────

    // ✅ PaymentDto → PaymentStatusResponse
    Task<PaymentStatusResponse?> GetPaymentByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    // ✅ PaymentSummaryDto → PaymentSummaryResponse
    Task<PagedResponse<PaymentSummaryResponse>?> GetPaymentsByCustomerAsync(
        string customerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    // ── Reconciliation ────────────────────────────────────────────────────────

    Task<Guid> TriggerReconciliationAsync(
        string provider,
        DateOnly reconDate,
        CancellationToken cancellationToken = default);

    Task<ReconciliationReportResponse?> GetReconciliationReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<DiscrepancyResponse>?> GetDiscrepanciesAsync(
        Guid reportId,
        bool unresolvedOnly = true,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<bool> ResolveDiscrepancyAsync(
        Guid discrepancyId,
        string resolvedBy,
        string notes,
        CancellationToken cancellationToken = default);

    // ── Webhooks ──────────────────────────────────────────────────────────────

    Task<PagedResponse<WebhookEventResponse>?> GetWebhookEventsAsync(
        string? provider = null,
        bool? processed = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<WebhookEventResponse?> GetWebhookEventAsync(
        Guid webhookEventId,
        CancellationToken cancellationToken = default);

    Task<bool> ReplayWebhookAsync(
        Guid webhookEventId,
        string reason,
        CancellationToken cancellationToken = default);
}