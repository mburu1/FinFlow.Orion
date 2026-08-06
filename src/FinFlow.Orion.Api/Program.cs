using FinFlow.Orion.Api.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("[FinFlow.Orion] Starting up...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseApiMiddleware();

    Log.Information("[FinFlow.Orion] Running on {Urls}", string.Join(", ", builder.WebHost.GetSetting("urls") ?? "default"));

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "[FinFlow.Orion] Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}