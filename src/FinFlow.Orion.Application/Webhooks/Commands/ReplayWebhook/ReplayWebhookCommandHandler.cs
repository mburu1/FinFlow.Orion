using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Webhooks.Commands.ReplayWebhook;

public sealed class ReplayWebhookCommandHandler
    : IRequestHandler<ReplayWebhookCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebhookRepository _webhookRepository;
    private readonly ILogger<ReplayWebhookCommandHandler> _logger;

    public ReplayWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        IWebhookRepository webhookRepository,
        ILogger<ReplayWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _webhookRepository = webhookRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ReplayWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var webhookEvent = await _webhookRepository
            .GetByIdAsync(request.WebhookEventId, cancellationToken)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.Webhooks.WebhookEvent), request.WebhookEventId);

        webhookEvent.Replay();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Webhook] Replayed event {Id} by {By} — Provider: {Provider} | Reason: {Reason}",
            webhookEvent.Id,
            request.ReplayedBy,
            webhookEvent.Provider,
            request.Reason ?? "Not specified");

        return true;
    }
}