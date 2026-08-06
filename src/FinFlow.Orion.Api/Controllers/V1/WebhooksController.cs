using Asp.Versioning;
using FinFlow.Orion.Application;
using FinFlow.Orion.Application.Webhooks.Commands.ReplayWebhook;
using FinFlow.Orion.Contracts.Webhooks.Requests;
using FinFlow.Orion.Domain.Entities.Webhooks;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Infrastructure.Webhooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Orion.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMongoWebhookService _mongoWebhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IMediator mediator,
        IMongoWebhookService mongoWebhookService,
        ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _mongoWebhookService = mongoWebhookService;
        _logger = logger;
    }

    /// <summary>Receives raw M-Pesa webhook callbacks.</summary>
    [HttpPost("mpesa")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MpesaWebhook(CancellationToken cancellationToken)
        => await IngestWebhookAsync(PaymentProvider.MPesa, cancellationToken);

    /// <summary>Receives raw card provider webhook callbacks.</summary>
    [HttpPost("card")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CardWebhook(CancellationToken cancellationToken)
        => await IngestWebhookAsync(PaymentProvider.Card, cancellationToken);

    /// <summary>Receives raw bank transfer webhook callbacks.</summary>
    [HttpPost("bank")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BankWebhook(CancellationToken cancellationToken)
        => await IngestWebhookAsync(PaymentProvider.BankTransfer, cancellationToken);

    /// <summary>Replays a failed webhook event through the processing pipeline.</summary>
    [HttpPost("{webhookEventId:guid}/replay")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayWebhook(
        Guid webhookEventId,
        [FromBody] ReplayWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReplayWebhookCommand(
                webhookEventId,
                request.ReplayedBy,
                request.Reason),
            cancellationToken);

        return Ok(new { success = result });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<IActionResult> IngestWebhookAsync(
        PaymentProvider provider,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            _logger.LogWarning("[Webhook] Empty payload from {Provider}", provider);
            return BadRequest(new { error = "Empty payload." });
        }

        var webhookEvent = WebhookEvent.Create(
            provider: provider,
            eventType: WebhookEventType.PaymentCompleted,
            rawPayload: rawPayload);

        await _mongoWebhookService.StoreRawPayloadAsync(
            webhookEvent.Id, provider, rawPayload, cancellationToken);

        _logger.LogInformation(
            "[Webhook] Ingested {Provider} payload — EventId: {Id}",
            provider, webhookEvent.Id);

        return Ok(new { received = true, eventId = webhookEvent.Id });
    }
}