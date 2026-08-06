using FinFlow.Orion.Infrastructure.Persistence.Outbox;
using FinFlow.Orion.Workers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FinFlow.Orion.Workers.Jobs;

[DisallowConcurrentExecution]
public sealed class OutboxProcessorJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorJob> _logger;

    public OutboxProcessorJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkerOutboxPublisher>();

        _logger.LogInformation("[OutboxProcessorJob] Polling for pending messages...");

        var messages = await outboxService.GetPendingMessagesAsync(
            batchSize: 20,
            cancellationToken: context.CancellationToken);

        if (messages.Count == 0)
        {
            _logger.LogDebug("[OutboxProcessorJob] No pending messages found.");
            return;
        }

        _logger.LogInformation(
            "[OutboxProcessorJob] Found {Count} pending messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation(
                    "[OutboxProcessorJob] Processing — Id: {Id} | Type: {Type}",
                    message.Id, message.Type);

                message.StartProcessing();

                await publisher.PublishAsync(
                    message.Type,
                    message.Payload,
                    context.CancellationToken);

                await outboxService.MarkAsProcessedAsync(
                    message.Id, context.CancellationToken);

                _logger.LogInformation(
                    "[OutboxProcessorJob] Published — Id: {Id}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[OutboxProcessorJob] Failed — Id: {Id} | Type: {Type}",
                    message.Id, message.Type);

                await outboxService.MarkAsFailedAsync(
                    message.Id, ex.Message, context.CancellationToken);
            }
        }

        _logger.LogInformation(
            "[OutboxProcessorJob] Done — Processed batch of {Count}.", messages.Count);
    }
}