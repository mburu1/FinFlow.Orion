
using Serilog;
using Serilog.Formatting.Compact;


// ── Bootstrap logger (captures startup failures before appsettings loads) ────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/webhooks-bootstrap-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        formatter: new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("[FinFlow.Orion.Webhooks] Starting up...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "FinFlow.Orion.Webhooks"));

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "[FinFlow.Orion.Webhooks] HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    var urls = builder.WebHost.GetSetting("urls") ?? "default";
    Log.Information("[FinFlow.Orion.Webhooks] Running on {Urls}", urls);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "[FinFlow.Orion.Webhooks] Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}