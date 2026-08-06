using FinFlow.Orion.Api.Middleware;

namespace FinFlow.Orion.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        // ── Exception handling — must be first ────────────────────────────────
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // ── Swagger ───────────────────────────────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "FinFlow.Orion v1");
                options.RoutePrefix = string.Empty; // Swagger at root
            });
        }

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