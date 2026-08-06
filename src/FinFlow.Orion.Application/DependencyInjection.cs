using FinFlow.Orion.Application.Common.Behaviors;
using FinFlow.Orion.Application.Sagas;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FinFlow.Orion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR — register all handlers, notifications in this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviours — order matters
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation — register all validators in this assembly
        services.AddValidatorsFromAssembly(assembly);

        // Saga orchestrator
        services.AddScoped<IPaymentSagaOrchestrator, PaymentSaga>();

        return services;
    }
}