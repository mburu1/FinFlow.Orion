using FinFlow.Orion.Infrastructure.Persistence.Outbox;
using FinFlow.Orion.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FinFlow.Orion.Infrastructure.Jobs;

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
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

        _logger.LogInformation("[OutboxProcessorJob] Processing pending outbox messages...");

        var messages = await outboxService.GetPendingMessagesAsync(
            batchSize: 20,
            cancellationToken: context.CancellationToken);

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
            "[OutboxProcessorJob] Done — Processed {Count} messages.", messages.Count);
    }
}