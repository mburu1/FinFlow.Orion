using FinFlow.Orion.Api.Models;
using FinFlow.Orion.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinFlow.Orion.Api.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .ToArray());

            var problem = new ProblemDetailsResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "See the errors field for details.",
                Errors = errors
            };

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}