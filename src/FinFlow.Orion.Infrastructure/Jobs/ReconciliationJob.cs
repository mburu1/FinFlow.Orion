using FinFlow.Orion.Application.Reconciliation.Commands.TriggerReconciliation;
using FinFlow.Orion.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FinFlow.Orion.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class ReconciliationJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReconciliationJob> _logger;

    public ReconciliationJob(
        IServiceProvider serviceProvider,
        ILogger<ReconciliationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var provider = context.JobDetail.JobDataMap.GetString("Provider");
        if (string.IsNullOrWhiteSpace(provider)
            || !Enum.TryParse<PaymentProvider>(provider, true, out _))
        {
            _logger.LogError("[ReconciliationJob] Invalid provider: {Provider}", provider);
            return;
        }

        var reconDate = context.JobDetail.JobDataMap.GetString("ReconDate");
        if (string.IsNullOrWhiteSpace(reconDate)
            || !DateOnly.TryParse(reconDate, out var date))
        {
            _logger.LogError("[ReconciliationJob] Invalid ReconDate: {ReconDate}", reconDate);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        _logger.LogInformation(
            "[ReconciliationJob] Triggering reconciliation — Provider: {Provider} | Date: {Date}",
            provider, date);

        var reportId = await mediator.Send(
            new TriggerReconciliationCommand(provider, date, "Scheduler"),
            context.CancellationToken);

        _logger.LogInformation(
            "[ReconciliationJob] Reconciliation triggered — ReportId: {ReportId}", reportId);
    }
}