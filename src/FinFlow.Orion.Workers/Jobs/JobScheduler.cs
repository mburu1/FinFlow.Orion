using FinFlow.Orion.Domain.Enums;
using Microsoft.Extensions.Logging;
using Quartz;
using static Quartz.MisfireInstruction;

namespace FinFlow.Orion.Workers.Jobs;

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

        await ScheduleReconciliationJobsAsync(scheduler, cancellationToken);
        await ScheduleOutboxProcessorJobAsync(scheduler, cancellationToken);

        _logger.LogInformation("[JobScheduler] All jobs scheduled successfully.");
    }

    // ── Reconciliation — one job per provider, runs daily at 01:00 UTC ───────

    private async Task ScheduleReconciliationJobsAsync(
        IScheduler scheduler,
        CancellationToken cancellationToken)
    {
        var providers = Enum.GetNames<PaymentProvider>();

        foreach (var provider in providers)
        {
            var jobKey = new JobKey($"ReconciliationJob-{provider}", "ReconciliationGroup");

            if (await scheduler.CheckExists(jobKey, cancellationToken))
            {
                _logger.LogDebug(
                    "[JobScheduler] ReconciliationJob for {Provider} already scheduled.", provider);
                continue;
            }

            var job = JobBuilder.Create<ReconciliationJob>()
                .WithIdentity(jobKey)
                .UsingJobData("Provider", provider)
                .UsingJobData("ReconDate",
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd"))
                .StoreDurably()
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"ReconciliationTrigger-{provider}", "ReconciliationGroup")
                .WithDailyTimeIntervalSchedule(s => s
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 0)))
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            _logger.LogInformation(
                "[JobScheduler] ReconciliationJob scheduled for {Provider} — daily at 01:00 UTC.",
                provider);
        }
    }

    // ── Outbox Processor — runs every 30 seconds ──────────────────────────────

    private async Task ScheduleOutboxProcessorJobAsync(
        IScheduler scheduler,
        CancellationToken cancellationToken)
    {
        var jobKey = new JobKey("OutboxProcessorJob", "OutboxGroup");

        if (await scheduler.CheckExists(jobKey, cancellationToken))
        {
            _logger.LogDebug("[JobScheduler] OutboxProcessorJob already scheduled.");
            return;
        }

        var job = JobBuilder.Create<OutboxProcessorJob>()
            .WithIdentity(jobKey)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("OutboxProcessorTrigger", "OutboxGroup")
            .StartNow()
            .WithSimpleSchedule(s => s
                .WithIntervalInSeconds(30)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation(
            "[JobScheduler] OutboxProcessorJob scheduled — every 30 seconds.");
    }
}