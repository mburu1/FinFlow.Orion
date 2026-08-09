using System.Diagnostics;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Webhooks.Models;
using FinFlow.Orion.Webhooks.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace FinFlow.Orion.Webhooks.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Generic inbound webhook endpoint, keyed by provider route segment
    /// (e.g. POST /api/webhooks/mpesa, /api/webhooks/card, /api/webhooks/bank).
    /// Reads the raw request body directly rather than binding to a DTO —
    /// signature verification requires the exact byte stream, not a
    /// re-serialized model, and provider payload shapes vary too much to
    /// share one binding contract.
    /// </summary>
    [HttpPost("{provider}")]
    [Consumes("application/json")]
    public async Task<IActionResult> ReceiveAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        var webhookEventId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Every log line emitted within this scope — including from
        // WebhookService and anything it calls — automatically carries
        // WebhookEventId and Provider without passing them explicitly.
        using (LogContext.PushProperty("WebhookEventId", webhookEventId))
        using (LogContext.PushProperty("ProviderRoute", provider))
        using (LogContext.PushProperty("RemoteIp", HttpContext.Connection.RemoteIpAddress?.ToString()))
        {
            _logger.LogInformation(
                "[WebhooksController] Inbound webhook received — Route: {Route} | ContentLength: {ContentLength} | UserAgent: {UserAgent}",
                Request.Path.Value,
                Request.ContentLength,
                Request.Headers.UserAgent.ToString());

            if (!Enum.TryParse<PaymentProvider>(provider, ignoreCase: true, out var paymentProvider))
            {
                _logger.LogWarning(
                    "[WebhooksController] Rejected — unknown provider route segment: {Provider}",
                    provider);
                return NotFound();
            }

            using (LogContext.PushProperty("Provider", paymentProvider))
            {
                Request.EnableBuffering();

                string rawBody;
                using (var reader = new StreamReader(Request.Body, leaveOpen: true))
                {
                    rawBody = await reader.ReadToEndAsync(cancellationToken);
                }
                Request.Body.Position = 0;

                _logger.LogDebug(
                    "[WebhooksController] Raw body captured — {ByteCount} bytes",
                    rawBody.Length);

                var signature = Request.Headers.TryGetValue("X-Signature", out var sigValues)
                    ? sigValues.ToString()
                    : null;

                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning(
                        "[WebhooksController] No X-Signature header present on inbound webhook — Provider: {Provider}",
                        paymentProvider);
                }
                else
                {
                    _logger.LogDebug(
                        "[WebhooksController] Signature header present — Length: {SignatureLength}",
                        signature.Length);
                }

                var headers = Request.Headers.ToDictionary(
                    h => h.Key,
                    h => h.Value.ToString());

                // Note: Microsoft.Extensions.Logging has no "Verbose" level by name —
                // LogTrace is the lowest built-in level and maps to Serilog's
                // Verbose under the standard MEL-to-Serilog level mapping.
                _logger.LogTrace(
                    "[WebhooksController] Full header dump — {@Headers}",
                    headers);

                var payload = new WebhookPayload
                {
                    WebhookEventId = webhookEventId,
                    Provider = paymentProvider,
                    RawBody = rawBody,
                    Signature = signature,
                    Headers = headers,
                    ReceivedAt = DateTime.UtcNow
                };

                bool accepted;
                try
                {
                    accepted = await _webhookService.ProcessInboundAsync(payload, cancellationToken);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex,
                        "[WebhooksController] Unhandled exception processing webhook — Provider: {Provider} | ElapsedMs: {ElapsedMs}",
                        paymentProvider, stopwatch.Elapsed.TotalMilliseconds);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                stopwatch.Stop();

                if (!accepted)
                {
                    _logger.LogWarning(
                        "[WebhooksController] Webhook rejected (signature verification failed) — Provider: {Provider} | ElapsedMs: {ElapsedMs}",
                        paymentProvider, stopwatch.Elapsed.TotalMilliseconds);
                    return Unauthorized();
                }

                _logger.LogInformation(
                    "[WebhooksController] Webhook accepted and processed — Provider: {Provider} | ElapsedMs: {ElapsedMs}",
                    paymentProvider, stopwatch.Elapsed.TotalMilliseconds);

                return Ok();
            }
        }
    }
}