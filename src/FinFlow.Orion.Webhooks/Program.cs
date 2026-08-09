using FinFlow.Orion.Application;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Infrastructure.Logging;
using FinFlow.Orion.Webhooks.Parsing;
using FinFlow.Orion.Webhooks.Security;
using FinFlow.Orion.Webhooks.Services;
using Serilog;

// ── Bootstrap logger (captures startup failures before appsettings loads) ────
Log.Logger = SerilogConfiguration.CreateBootstrapLogger("FinFlow.Orion.Webhooks");

try
{
    Log.Information("[FinFlow.Orion.Webhooks] Starting up...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Services.AddSerilogVerboseLogging(builder.Configuration, "FinFlow.Orion.Webhooks");
    builder.Host.UseSerilog();

    // ── Application services ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // AddApplication() must run before AddInfrastructure() — ApplicationDbContext
    // and several Infrastructure services depend on IMediator, which is
    // registered by AddApplication's AddMediatR call.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<IWebhookService, WebhookService>();

    // ── Webhook security & parsing ───────────────────────────────────────────
    builder.Services.Configure<WebhookSecurityConfiguration>(
        builder.Configuration.GetSection(WebhookSecurityConfiguration.SectionName));
    builder.Services.AddScoped<IWebhookSignatureVerifier, WebhookSignatureVerifier>();
    builder.Services.AddScoped<IWebhookPayloadParser, WebhookPayloadParser>();

    var app = builder.Build();

    Log.Information("[FinFlow.Orion.Webhooks] Host built successfully. Configuring pipeline...");

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