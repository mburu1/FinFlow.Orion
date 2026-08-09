using System.Diagnostics;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Webhooks;
using FinFlow.Orion.Infrastructure.Webhooks;
using FinFlow.Orion.Webhooks.Models;
using FinFlow.Orion.Webhooks.Parsing;
using FinFlow.Orion.Webhooks.Security;

namespace FinFlow.Orion.Webhooks.Services;

public interface IWebhookService
{
    /// <summary>
    /// Verifies, persists, and dispatches an inbound webhook payload. Does not
    /// throw for malformed provider payloads — returns false so the controller can
    /// still ack the provider's delivery attempt (most providers retry aggressively
    /// on non-2xx responses, which we generally want to avoid unless the
    /// signature itself is invalid).
    /// </summary>
    Task<bool> ProcessInboundAsync(
        WebhookPayload payload,
        CancellationToken cancellationToken = default);
}

public sealed class WebhookService : IWebhookService
{
    private readonly IMongoWebhookService _mongoWebhookService;
    private readonly IWebhookSignatureVerifier _signatureVerifier;
    private readonly IWebhookPayloadParser _payloadParser;
    private readonly IWebhookRepository _webhookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IMongoWebhookService mongoWebhookService,
        IWebhookSignatureVerifier signatureVerifier,
        IWebhookPayloadParser payloadParser,
        IWebhookRepository webhookRepository,
        IUnitOfWork unitOfWork,
        ILogger<WebhookService> logger)
    {
        _mongoWebhookService = mongoWebhookService;
        _signatureVerifier = signatureVerifier;
        _payloadParser = payloadParser;
        _webhookRepository = webhookRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ProcessInboundAsync(
        WebhookPayload payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[WebhookService] Begin processing — EventId: {Id} | Provider: {Provider} | ReceivedAt: {ReceivedAt:O} | BodyLength: {BodyLength}",
            payload.WebhookEventId, payload.Provider, payload.ReceivedAt, payload.RawBody.Length);

        var verifyStopwatch = Stopwatch.StartNew();
        var signatureValid = _signatureVerifier.Verify(payload);
        verifyStopwatch.Stop();

        _logger.LogDebug(
            "[WebhookService] Signature verification completed — EventId: {Id} | Valid: {Valid} | ElapsedMs: {ElapsedMs}",
            payload.WebhookEventId, signatureValid, verifyStopwatch.Elapsed.TotalMilliseconds);

        if (!signatureValid)
        {
            _logger.LogWarning(
                "[WebhookService] Signature verification failed — rejecting — EventId: {Id} | Provider: {Provider} | SignaturePresent: {SignaturePresent}",
                payload.WebhookEventId, payload.Provider, !string.IsNullOrEmpty(payload.Signature));
            return false;
        }

        var storeStopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug(
                "[WebhookService] Storing raw payload to MongoDB — EventId: {Id} | Provider: {Provider}",
                payload.WebhookEventId, payload.Provider);

            await _mongoWebhookService.StoreRawPayloadAsync(
                payload.WebhookEventId,
                payload.Provider,
                payload.RawBody,
                cancellationToken);

            storeStopwatch.Stop();

            _logger.LogInformation(
                "[WebhookService] Stored inbound webhook successfully — EventId: {Id} | Provider: {Provider} | MongoWriteMs: {MongoWriteMs}",
                payload.WebhookEventId, payload.Provider, storeStopwatch.Elapsed.TotalMilliseconds);

            var parsed = _payloadParser.Parse(payload);

            var webhookEvent = WebhookEvent.Create(
                payload.Provider,
                parsed.EventType,
                payload.RawBody,
                parsed.PaymentReference,
                parsed.ProviderTransactionId);

            await _webhookRepository.AddAsync(webhookEvent, cancellationToken);

            // Persists the WebhookEvent, dispatches WebhookReceivedEvent to its
            // in-process handler, and writes the corresponding outbox row so
            // Workers can publish WebhookReceivedIntegrationEvent onto the bus.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[WebhookService] Recorded WebhookEvent {WebhookEventId} — EventType: {EventType} | ProviderTxId: {TxId}",
                webhookEvent.Id, parsed.EventType, parsed.ProviderTransactionId);

            return true;
        }
        catch (Exception ex)
        {
            storeStopwatch.Stop();
            _logger.LogError(ex,
                "[WebhookService] Failed to process inbound webhook — EventId: {Id} | Provider: {Provider} | ElapsedMsBeforeFailure: {ElapsedMs}",
                payload.WebhookEventId, payload.Provider, storeStopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
