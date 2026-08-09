using FinFlow.Orion.Application.Payments.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinFlow.Orion.Infrastructure.Messaging;

/// <summary>
/// Registers the MassTransit bus (RabbitMQ transport) and its consumers. Called
/// only from FinFlow.Orion.Workers — Api and Webhooks never touch the bus directly,
/// they only write outbox rows via ApplicationDbContext.SaveChangesAsync.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<PaymentInitiatedIntegrationEventConsumer>();
            x.AddConsumer<PaymentFailedIntegrationEventConsumer>();
            // WebhookReceivedIntegrationEventConsumer is registered here too — see
            // Webhooks/Consumers/WebhookReceivedIntegrationEventConsumer.cs.
            x.AddConsumer<FinFlow.Orion.Application.Webhooks.Consumers.WebhookReceivedIntegrationEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
                var username = configuration["RabbitMq:Username"] ?? "guest";
                var password = configuration["RabbitMq:Password"] ?? "guest";
                var port = configuration.GetValue<ushort?>("RabbitMq:Port") ?? 5672;

                cfg.Host(host, port, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
