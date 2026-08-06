using FinFlow.Orion.Domain.Events.Reconciliation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Reconciliation.EventHandlers;

public sealed class ReconciliationCompletedEventHandler
    : INotificationHandler<ReconciliationCompletedEvent>
{
    private readonly ILogger<ReconciliationCompletedEventHandler> _logger;

    public ReconciliationCompletedEventHandler(
        ILogger<ReconciliationCompletedEventHandler> logger)
        => _logger = logger;

    public Task Handle(
        ReconciliationCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] ReconciliationCompleted — Report: {Ref} | Provider: {Provider} | Matched: {Matched} | Unmatched: {Unmatched}",
            notification.ReportReference,
            notification.Provider,
            notification.MatchedCount,
            notification.UnmatchedCount);

        return Task.CompletedTask;
    }
}