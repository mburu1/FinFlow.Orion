using FinFlow.Orion.Api.Models;
using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace FinFlow.Orion.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ExceptionHandler] Unhandled exception — Path: {Path} | Method: {Method}",
                context.Request.Path, context.Request.Method);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, problem) = exception switch
        {
            NotFoundException ex => (
                HttpStatusCode.NotFound,
                new ProblemDetailsResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource not found.",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    TraceId = traceId
                }),

            Application.Common.Exceptions.ValidationException ex => (
                HttpStatusCode.BadRequest,
                new ProblemDetailsResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message,
                    Errors = ex.Errors,
                    TraceId = traceId
                }),

            IdempotencyViolationException ex => (
                HttpStatusCode.Conflict,
                new ProblemDetailsResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Duplicate request detected.",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    TraceId = traceId
                }),

            DomainException ex => (
                HttpStatusCode.UnprocessableEntity,
                new ProblemDetailsResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Domain rule violation.",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = ex.Message,
                    TraceId = traceId
                }),

            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetailsResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An internal server error occurred. Please try again later.",
                    TraceId = traceId
                })
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}