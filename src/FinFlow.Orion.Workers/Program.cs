using FinFlow.Orion.Application;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Infrastructure.Logging;
using FinFlow.Orion.Infrastructure.Messaging;
using FinFlow.Orion.Workers.Jobs;
using FinFlow.Orion.Workers.Services;
using Quartz;
using Serilog;

// ── Bootstrap logger (captures startup failures before appsettings loads) ────
Log.Logger = SerilogConfiguration.CreateBootstrapLogger("FinFlow.Orion.Workers");

try
{
    Log.Information("[FinFlow.Orion.Workers] Starting up...");

    var builder = Host.CreateApplicationBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Services.AddSerilogVerboseLogging(builder.Configuration, "FinFlow.Orion.Workers");

    // ── Application + Infrastructure ──────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Worker services ───────────────────────────────────────────────────────
    builder.Services.AddScoped<IDateTimeService, DateTimeService>();
    builder.Services.AddScoped<IWorkerOutboxPublisher, WorkerOutboxPublisher>();
    builder.Services.AddScoped<JobScheduler>();

    // ── Messaging (MassTransit / RabbitMQ) — bus + consumers live only here ────
    // MassTransit 9+ requires a license at bus-build time: set the MT_LICENSE
    // (or MT_LICENSE_PATH) environment variable before running this process,
    // otherwise startup fails with MassTransit.ConfigurationException. See the
    // README's "Known limitations" section.
    builder.Services.AddMessaging(builder.Configuration);

    // ── Quartz ────────────────────────────────────────────────────────────────
    builder.Services.AddQuartz();

    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });

    // ── Hosted service — schedules all Quartz jobs on startup ─────────────────
    builder.Services.AddHostedService<JobSchedulerHostedService>();

    var host = builder.Build();

    Log.Information("[FinFlow.Orion.Workers] Host built successfully. Starting workers...");

    await host.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "[FinFlow.Orion.Workers] Terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// ── Hosted service wrapper ────────────────────────────────────────────────────

public sealed class JobSchedulerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerHostedService> _logger;

    public JobSchedulerHostedService(
        IServiceProvider serviceProvider,
        ILogger<JobSchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[JobSchedulerHostedService] Scheduling all jobs...");

        using var scope = _serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<JobScheduler>();
        await scheduler.ScheduleAllJobsAsync(cancellationToken);

        _logger.LogInformation("[JobSchedulerHostedService] All jobs registered successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[JobSchedulerHostedService] Shutting down gracefully.");
        return Task.CompletedTask;
    }
}