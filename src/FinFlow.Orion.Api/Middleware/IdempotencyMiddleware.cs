using FinFlow.Orion.Application.Common.Interfaces;

namespace FinFlow.Orion.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    // Only enforce idempotency on state-changing methods
    private static readonly HashSet<string> IdempotentMethods =
        ["POST", "PATCH", "DELETE"];

    public IdempotencyMiddleware(
        RequestDelegate next,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        if (!IdempotentMethods.Contains(context.Request.Method.ToUpper()))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        // Check for cached response
        var cachedResponse = await idempotencyService.GetAsync(idempotencyKey);
        if (cachedResponse is not null)
        {
            _logger.LogWarning(
                "[Idempotency] Duplicate request detected — Key: {Key} | Path: {Path}",
                idempotencyKey, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        await _next(context);
    }
}