using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using FinFlow.Orion.Infrastructure.Persistence.Configurations;
using Serilog.Settings.Configuration;
using Serilog.Exceptions;
using Serilog.Extensions.Hosting;
using Serilog.Sinks;
using Serilog.Sinks.File;



namespace FinFlow.Orion.Infrastructure.Logging;

/// <summary>
/// Centralized Serilog configuration for FinFlow.Orion services (Api, Workers, etc.).
/// Captures everything at Verbose level to rolling, size-capped files, with a
/// separate compact-JSON sink for machine parsing / log aggregation, plus a
/// human-readable console sink.
///
/// Usage (Program.cs, top-level statements, .NET 10 minimal hosting):
///
///     var builder = Host.CreateApplicationBuilder(args);
///     builder.Services.AddSerilogVerboseLogging(builder.Configuration, "FinFlow.Orion.Workers");
///     ...
///     var host = builder.Build();
///     await host.RunAsync();
///
/// For WebApplicationBuilder (Api project) the same extension applies — it
/// operates on IServiceCollection + IConfiguration, not the builder type.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Registers Serilog as the logging provider with verbose, multi-sink output.
    /// Call this once during host/application startup, before Build().
    /// </summary>
    /// <param name="services">The service collection from the host builder.</param>
    /// <param name="configuration">The bound IConfiguration (appsettings.json etc.).</param>
    /// <param name="applicationName">
    /// Logical service name, enriched onto every log event. Used in log file
    /// naming and to distinguish services (Api, Workers) in aggregated logs.
    /// </param>
    /// <param name="logDirectory">
    /// Directory (relative to content root, or absolute) where rolling log
    /// files are written. Defaults to "logs".
    /// </param>
    public static IServiceCollection AddSerilogVerboseLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        string logDirectory = "logs")
    {
        var readableLogPath = Path.Combine(logDirectory, $"{applicationName}-.log");
        var jsonLogPath = Path.Combine(logDirectory, $"{applicationName}-.json.log");

        services.AddSerilog((sp, loggerConfiguration) =>
        {
            loggerConfiguration
                // ── Base level: capture everything; per-sink minimum levels below
                // narrow what actually gets written where. ─────────────────────
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Debug)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
                .MinimumLevel.Override("Quartz", LogEventLevel.Debug)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                // Explicitly keep our own code at Verbose — nothing above should
                // shadow this, but being explicit avoids surprises if a broader
                // "Microsoft"/"System" override is ever widened later.
                .MinimumLevel.Override("FinFlow.Orion", LogEventLevel.Verbose)
                // Read any overrides from appsettings.json ("Serilog" section),
                // applied after the above so config always wins if present.
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(sp)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProcessId()
                .Enrich.WithEnvironmentName()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Application", applicationName)

                // ── Console: human-readable, Information and above ──────────────
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}")

                // ── Rolling readable file: everything, verbose, for local debugging ──
                .WriteTo.File(
                    path: readableLogPath,
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100 MB
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}")

                // ── Rolling compact JSON file: everything, for log shipping / parsing ──
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: jsonLogPath,
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 200 * 1024 * 1024, // 200 MB
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 14,
                    shared: true)

                // ── Dedicated error/fatal file for fast incident triage ─────────
                .WriteTo.File(
                    path: Path.Combine(logDirectory, $"{applicationName}-errors-.log"),
                    restrictedToMinimumLevel: LogEventLevel.Warning,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}");
        });

        return services;
    }

    /// <summary>
    /// Builds a bootstrap logger for capturing failures that occur before the
    /// full host/DI container is available (config load, DI wiring, etc.).
    /// Call Log.CloseAndFlush() in a finally block once host.RunAsync() completes.
    /// </summary>
    /// <param name="applicationName">Logical service name for the bootstrap log file.</param>
    /// <param name="logDirectory">Directory for the bootstrap log file.</param>
    public static Serilog.ILogger CreateBootstrapLogger(
        string applicationName,
        string logDirectory = "logs")
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDirectory, $"{applicationName}-bootstrap-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatter: new CompactJsonFormatter())
            .CreateBootstrapLogger();
    }
}