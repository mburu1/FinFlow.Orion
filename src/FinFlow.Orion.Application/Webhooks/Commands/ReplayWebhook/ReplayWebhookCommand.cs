using MediatR;

namespace FinFlow.Orion.Application.Webhooks.Commands.ReplayWebhook;

public sealed record ReplayWebhookCommand(
    Guid WebhookEventId,
    string ReplayedBy,
    string? Reason = null
) : IRequest<bool>;