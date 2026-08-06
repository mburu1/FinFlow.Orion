using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Infrastructure.Idempotency;
using FinFlow.Orion.Infrastructure.Jobs;
using FinFlow.Orion.Infrastructure.Persistence;
using FinFlow.Orion.Infrastructure.Persistence.Outbox;
using FinFlow.Orion.Infrastructure.Persistence.Repositories;
using FinFlow.Orion.Infrastructure.Providers.Bank;
using FinFlow.Orion.Infrastructure.Providers.Card;
using FinFlow.Orion.Infrastructure.Providers.MPesa;
using FinFlow.Orion.Infrastructure.Services;
using FinFlow.Orion.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Refit;

namespace FinFlow.Orion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DbContexts ────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
        });

        services.AddDbContext<LedgerDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
        });

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IOutboxService, OutboxService>();

        // ── Providers ─────────────────────────────────────────────────────────
        services.Configure<MpesaConfiguration>(
            configuration.GetSection(MpesaConfiguration.SectionName));
        services.AddScoped<IMpesaProvider, MpesaProvider>();
        services.AddScoped<ICardProvider, CardProvider>();
        services.AddScoped<IBankProvider, BankProvider>();

        // ── Refit — M-Pesa HTTP client ────────────────────────────────────────
        services.AddRefitClient<IMpesaClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var config = sp.GetRequiredService<IOptions<MpesaConfiguration>>().Value;
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            });

        // ── MongoDB / Webhooks ────────────────────────────────────────────────
        services.Configure<MongoWebhookConfiguration>(
            configuration.GetSection(MongoWebhookConfiguration.SectionName));
        services.AddScoped<IMongoWebhookService, MongoWebhookService>();

        // ── Idempotency ───────────────────────────────────────────────────────
        services.AddScoped<IIdempotencyService, SqlIdempotencyService>();

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();

        // ── Quartz ────────────────────────────────────────────────────────────────
        services.AddQuartz(q =>
        {
            // UseMicrosoftDependencyInjectionJobFactory() removed —
            // it is the default in Quartz 3.6+ and is now obsolete
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddScoped<JobScheduler>();

        // JobScheduler is now an injectable class, not static
        services.AddScoped<JobScheduler>();

        return services;
    }
}