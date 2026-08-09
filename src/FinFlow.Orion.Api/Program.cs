using FinFlow.Orion.Api.Extensions;
using FinFlow.Orion.Api.Middleware;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

// ============================================================================
// BOOTSTRAP LOGGER
//
// Captures startup failures before appsettings.json and the DI container
// are fully configured.
// ============================================================================

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(
        outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/api-bootstrap-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        formatter: new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information(
        "[FinFlow.Orion.Api] Starting up...");

    // =========================================================================
    // BUILDER
    // =========================================================================

    var builder = WebApplication.CreateBuilder(args);

    // =========================================================================
    // SERILOG
    // =========================================================================

    builder.Host.UseSerilog(
        (context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty(
                    "Application",
                    "FinFlow.Orion.Api");
        });

    // =========================================================================
    // APPLICATION SERVICES
    //
    // AddApiServices() is the central registration point for:
    //
    // - Controllers
    // - API versioning
    // - OpenAPI
    // - JWT authentication
    // - Authorization
    // - CORS
    // - Application layer
    // - Infrastructure layer
    // - Ledger
    // - MongoDB
    // - Health checks
    //
    // JWT authentication MUST NOT be registered again here.
    // =========================================================================

    builder.Services.AddApiServices(
        builder.Configuration,
        builder.Environment);

    // =========================================================================
    // BUILD APPLICATION
    // =========================================================================

    var app = builder.Build();

    // =========================================================================
    // SERILOG HTTP REQUEST LOGGING
    // =========================================================================

    app.UseSerilogRequestLogging(
        options =>
        {
            options.MessageTemplate =
                "[FinFlow.Orion.Api] HTTP {RequestMethod} {RequestPath} " +
                "responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext =
                (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set(
                        "RequestHost",
                        httpContext.Request.Host.Value);

                    diagnosticContext.Set(
                        "RequestScheme",
                        httpContext.Request.Scheme);

                    diagnosticContext.Set(
                        "UserAgent",
                        httpContext.Request.Headers.UserAgent.ToString());

                    diagnosticContext.Set(
                        "RemoteIp",
                        httpContext.Connection.RemoteIpAddress?.ToString());

                    diagnosticContext.Set(
                        "TraceIdentifier",
                        httpContext.TraceIdentifier);
                };
        });

    // =========================================================================
    // DEVELOPMENT ENVIRONMENT
    //
    // Native .NET 10 OpenAPI document:
    //     /openapi/v1.json
    //
    // Scalar UI:
    //     /scalar/v1
    //
    // OpenAPI + JWT Bearer configuration is registered inside
    // ServiceCollectionExtensions.AddApiServices().
    // =========================================================================

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        app.MapScalarApiReference(
            options =>
            {
                options
                    .WithTitle("FinFlow Orion API")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(
                        ScalarTarget.CSharp,
                        ScalarClient.HttpClient);
            });
    }

    // =========================================================================
    // EXCEPTION HANDLING
    //
    // Must be the outermost middleware so it can catch exceptions from every
    // stage of the pipeline (auth, idempotency, controllers) and map them to
    // the correct ProblemDetails status code (404/409/422/500).
    // =========================================================================

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // =========================================================================
    // HTTPS
    // =========================================================================

    app.UseHttpsRedirection();

    // =========================================================================
    // CORS
    //
    // The "FinFlowCors" policy is registered by AddApiServices().
    // =========================================================================

    app.UseCors("FinFlowCors");

    // =========================================================================
    // AUTHENTICATION
    //
    // MUST execute before authorization.
    //
    // JWT Bearer authentication is registered by AddApiServices().
    // =========================================================================

    app.UseAuthentication();

    // =========================================================================
    // AUTHORIZATION
    //
    // Processes [Authorize] attributes and authorization policies.
    // =========================================================================

    app.UseAuthorization();

    // =========================================================================
    // IDEMPOTENCY
    //
    // Short-circuits duplicate POST/PATCH/DELETE requests carrying a repeated
    // Idempotency-Key header with the cached response, before they reach a
    // controller/handler.
    // =========================================================================

    app.UseMiddleware<IdempotencyMiddleware>();

    // =========================================================================
    // CONTROLLERS
    // =========================================================================

    app.MapControllers();

    // =========================================================================
    // HEALTH CHECKS
    //
    // GET /health
    //
    // Health checks are registered by AddApiServices().
    // =========================================================================

    app.MapHealthChecks("/health");

    // =========================================================================
    // APPLICATION STARTED
    // =========================================================================

    Log.Information(
        "[FinFlow.Orion.Api] Application configured successfully.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(
        ex,
        "[FinFlow.Orion.Api] Application terminated unexpectedly.");

    throw;
}
finally
{
    Log.Information(
        "[FinFlow.Orion.Api] Shutting down.");

    Log.CloseAndFlush();
}

// Exposes the top-level Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }