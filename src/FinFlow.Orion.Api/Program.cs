using Microsoft.AspNetCore.OpenApi;
using FinFlow.Orion.Api.Extensions;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

// ── Bootstrap logger (captures startup failures before appsettings loads) ────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/api-bootstrap-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        formatter: new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("[FinFlow.Orion.Api] Starting up...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "FinFlow.Orion.Api"));

    // ── Services ──────────────────────────────────────────────────────────────
    // AddApiServices() already registers OpenAPI (document "v1", Bearer + Idempotency-Key
    // transformers) — do NOT call builder.Services.AddOpenApi() again here. Two calls
    // targeting the same default document name collide at startup.
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "[FinFlow.Orion.Api] HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    // ── Scalar API UI (dev only) ──────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "FinFlow.Orion API";
            options.Theme = ScalarTheme.DeepSpace;
        });
    }

    app.UseApiMiddleware();

    var urls = builder.WebHost.GetSetting("urls") ?? "default";
    Log.Information("[FinFlow.Orion.Api] Running on {Urls}", urls);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "[FinFlow.Orion.Api] Application terminated unexpectedly.");

    // Without this, a fatal startup exception still exits 0 — orchestrators
    // (k8s liveness probes, systemd, Docker restart policies) read that as
    // success and won't restart the process or alert anyone.
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}