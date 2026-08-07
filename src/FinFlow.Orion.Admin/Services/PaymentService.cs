using FinFlow.Orion.Contracts.Common;
using FinFlow.Orion.Contracts.Payments.Responses;
using FinFlow.Orion.Contracts.Reconciliation.Requests;
using FinFlow.Orion.Contracts.Reconciliation.Responses;
using FinFlow.Orion.Contracts.Webhooks.Requests;
using FinFlow.Orion.Contracts.Webhooks.Responses;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Admin.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ApiClient apiClient, ILogger<PaymentService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    // ✅ PaymentDto → PaymentStatusResponse
    public async Task<PaymentStatusResponse?> GetPaymentByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[PaymentService] Fetching payment {PaymentId}", paymentId);

            return await _apiClient.GetAsync<PaymentStatusResponse>(
                $"/api/v1/payments/{paymentId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PaymentService] Error fetching payment {PaymentId}", paymentId);
            throw;
        }
    }

    // ✅ PaymentSummaryDto → PaymentSummaryResponse
    public async Task<PagedResponse<PaymentSummaryResponse>?> GetPaymentsByCustomerAsync(
        string customerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Fetching payments for customer {CustomerId}", customerId);

            return await _apiClient.GetAsync<PagedResponse<PaymentSummaryResponse>>(
                $"/api/v1/payments/customer/{customerId}?page={page}&pageSize={pageSize}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error fetching payments for customer {CustomerId}", customerId);
            throw;
        }
    }

    // ── Reconciliation ────────────────────────────────────────────────────────

    public async Task<Guid> TriggerReconciliationAsync(
    string provider,
    DateOnly reconDate,
    CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Triggering reconciliation — Provider: {Provider} | Date: {Date}",
                provider, reconDate);

            var request = new TriggerReconciliationRequest
            {
                Provider = provider,
                ReconDate = reconDate,
                TriggeredBy = "Admin"
            };

            return await _apiClient.PostAsync<Guid>(
                "/api/v1/reconciliation/trigger",
                request,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error triggering reconciliation — Provider: {Provider}", provider);
            throw;
        }
    }

    public async Task<ReconciliationReportResponse?> GetReconciliationReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Fetching reconciliation report {ReportId}", reportId);

            return await _apiClient.GetAsync<ReconciliationReportResponse>(
                $"/api/v1/reconciliation/{reportId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error fetching reconciliation report {ReportId}", reportId);
            throw;
        }
    }

    public async Task<PagedResponse<DiscrepancyResponse>?> GetDiscrepanciesAsync(
        Guid reportId,
        bool unresolvedOnly = true,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Fetching discrepancies for report {ReportId}", reportId);

            return await _apiClient.GetAsync<PagedResponse<DiscrepancyResponse>>(
                $"/api/v1/reconciliation/{reportId}/discrepancies?unresolvedOnly={unresolvedOnly}&page={page}&pageSize={pageSize}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error fetching discrepancies for report {ReportId}", reportId);
            throw;
        }
    }

    public async Task<bool> ResolveDiscrepancyAsync(
        Guid discrepancyId,
        string resolvedBy,
        string notes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Resolving discrepancy {DiscrepancyId} by {ResolvedBy}",
                discrepancyId, resolvedBy);

            var request = new ResolveDiscrepancyRequest
            {
                DiscrepancyId = discrepancyId,
                ResolvedBy = resolvedBy,
                Notes = notes
            };

            var response = await _apiClient.PostAsync<bool>(
                $"/api/v1/reconciliation/discrepancies/{discrepancyId}/resolve",
                request,
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error resolving discrepancy {DiscrepancyId}", discrepancyId);
            throw;
        }
    }

    // ── Webhooks ──────────────────────────────────────────────────────────────

    public async Task<PagedResponse<WebhookEventResponse>?> GetWebhookEventsAsync(
        string? provider = null,
        bool? processed = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(provider))
                query += $"&provider={provider}";
            if (processed.HasValue)
                query += $"&processed={processed.Value}";

            _logger.LogInformation("[PaymentService] Fetching webhook events");

            return await _apiClient.GetAsync<PagedResponse<WebhookEventResponse>>(
                $"/api/v1/webhooks?{query}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PaymentService] Error fetching webhook events");
            throw;
        }
    }

    public async Task<WebhookEventResponse?> GetWebhookEventAsync(
        Guid webhookEventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Fetching webhook event {WebhookEventId}", webhookEventId);

            return await _apiClient.GetAsync<WebhookEventResponse>(
                $"/api/v1/webhooks/{webhookEventId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error fetching webhook event {WebhookEventId}", webhookEventId);
            throw;
        }
    }

    public async Task<bool> ReplayWebhookAsync(
        Guid webhookEventId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[PaymentService] Replaying webhook {WebhookEventId} — Reason: {Reason}",
                webhookEventId, reason);

            var request = new ReplayWebhookRequest
            {
                WebhookEventId = webhookEventId,
                ReplayedBy = "Admin",
                Reason = reason
            };

            var response = await _apiClient.PostAsync<bool>(
                $"/api/v1/webhooks/{webhookEventId}/replay",
                request,
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService] Error replaying webhook {WebhookEventId}", webhookEventId);
            throw;
        }
    }
}