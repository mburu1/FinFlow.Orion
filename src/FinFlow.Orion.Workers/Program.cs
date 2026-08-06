using FinFlow.Orion.Application;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Ledger;
using FinFlow.Orion.Workers.Jobs;
using FinFlow.Orion.Workers.Services;
using Quartz;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("[FinFlow.Orion.Workers] Starting up...");

    var builder = Host.CreateApplicationBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Services.AddSerilog((services, config) =>
        config
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    // ── Application + Infrastructure + Ledger ─────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddLedger();

    // ── Worker services ───────────────────────────────────────────────────────
    builder.Services.AddScoped<IDateTimeService, DateTimeService>();
    builder.Services.AddScoped<IWorkerOutboxPublisher, WorkerOutboxPublisher>();
    builder.Services.AddScoped<JobScheduler>();

    // ── Quartz ────────────────────────────────────────────────────────────────
    builder.Services.AddQuartz();

    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });

    // ── Hosted service — schedules all Quartz jobs on startup ─────────────────
    builder.Services.AddHostedService<JobSchedulerHostedService>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
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
        using var scope = _serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<JobScheduler>();
        await scheduler.ScheduleAllJobsAsync(cancellationToken);
        _logger.LogInformation("[JobSchedulerHostedService] All jobs registered.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[JobSchedulerHostedService] Shutting down.");
        return Task.CompletedTask;
    }
}