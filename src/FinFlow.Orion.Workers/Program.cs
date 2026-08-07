using FinFlow.Orion.Application;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Ledger;
using FinFlow.Orion.Workers.Jobs;
using FinFlow.Orion.Workers.Services;
using Quartz;
using Serilog;
using Serilog.Formatting.Compact;

// ── Bootstrap logger (captures startup failures before appsettings loads) ────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/workers-bootstrap-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        formatter: new CompactJsonFormatter())
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
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "FinFlow.Orion.Workers"));

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