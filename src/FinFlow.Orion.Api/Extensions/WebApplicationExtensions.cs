using FinFlow.Orion.Api.Middleware;

namespace FinFlow.Orion.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        // ── Exception handling — must be first ────────────────────────────────
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // ── Middleware pipeline ───────────────────────────────────────────────
        app.UseHttpsRedirection();
        app.UseCors("FinFlowCors");
        app.UseAuthentication();
        app.UseAuthorization();

        // ── Idempotency ───────────────────────────────────────────────────────
        app.UseMiddleware<IdempotencyMiddleware>();

        // ── Routing ───────────────────────────────────────────────────────────
        app.MapControllers();

        // ── Health checks ─────────────────────────────────────────────────────
        app.MapHealthChecks("/health");

        return app;
    }
}