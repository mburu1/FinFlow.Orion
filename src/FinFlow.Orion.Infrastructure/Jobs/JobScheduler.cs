using FinFlow.Orion.Domain.Enums;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FinFlow.Orion.Infrastructure.Jobs;

public sealed class JobScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(
        ISchedulerFactory schedulerFactory,
        ILogger<JobScheduler> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task ScheduleAllJobsAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // ── Reconciliation Jobs ───────────────────────────────────────────────
        var providers = Enum.GetNames<PaymentProvider>();

        foreach (var provider in providers)
        {
            var jobKey = new JobKey($"ReconciliationJob-{provider}", "ReconciliationGroup");

            if (await scheduler.CheckExists(jobKey, cancellationToken))
                continue;

            var job = JobBuilder.Create<ReconciliationJob>()
                .WithIdentity(jobKey)
                .UsingJobData("Provider", provider)
                .UsingJobData("ReconDate", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("O"))
                .StoreDurably()
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"ReconciliationTrigger-{provider}", "ReconciliationGroup")
                .StartNow()
                .WithDailyTimeIntervalSchedule(x => x
                    .WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 0)))
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            _logger.LogInformation(
                "[JobScheduler] Scheduled ReconciliationJob for {Provider}", provider);
        }

        // ── Outbox Processor ─────────────────────────────────────────────────
        var outboxJobKey = new JobKey("OutboxProcessorJob", "OutboxGroup");

        if (!await scheduler.CheckExists(outboxJobKey, cancellationToken))
        {
            var outboxJob = JobBuilder.Create<OutboxProcessorJob>()
                .WithIdentity(outboxJobKey)
                .StoreDurably()
                .Build();

            var outboxTrigger = TriggerBuilder.Create()
                .WithIdentity("OutboxProcessorTrigger", "OutboxGroup")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(30)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(outboxJob, outboxTrigger, cancellationToken);

            _logger.LogInformation("[JobScheduler] Scheduled OutboxProcessorJob.");
        }

        _logger.LogInformation("[JobScheduler] All jobs scheduled successfully.");
    }
}