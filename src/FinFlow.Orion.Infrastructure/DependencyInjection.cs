using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Infrastructure.Idempotency;
using FinFlow.Orion.Infrastructure.Jobs;
using FinFlow.Orion.Infrastructure.Persistence; // For UnitOfWork, ApplicationDbContext, LedgerDbContext
using FinFlow.Orion.Infrastructure.Persistence.Outbox;
using FinFlow.Orion.Infrastructure.Persistence.Repositories; // For LedgerRepository, etc.
using FinFlow.Orion.Infrastructure.Providers.Bank;
using FinFlow.Orion.Infrastructure.Providers.Card;
using FinFlow.Orion.Infrastructure.Providers.MPesa;
using FinFlow.Orion.Infrastructure.Services;
using FinFlow.Orion.Infrastructure.Webhooks;
using FinFlow.Orion.Ledger.Abstractions;
using FinFlow.Orion.Ledger.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Refit;
using System;

namespace FinFlow.Orion.Infrastructure
{
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

            // ── Unit of Work & Repositories ───────────────────────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILedgerRepository, LedgerRepository>();
            services.AddScoped<IJournalEntryRepository, JournalEntryRepository>(); // ← ADDED THIS
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
            services.AddScoped<IWebhookRepository, WebhookRepository>();
            services.AddScoped<IOutboxService, OutboxService>();

            // ── Providers ─────────────────────────────────────────────────────────
            services.Configure<MpesaConfiguration>(configuration.GetSection(MpesaConfiguration.SectionName));
            services.AddScoped<IMpesaProvider, MpesaProvider>();

            services.Configure<CardConfiguration>(configuration.GetSection(CardConfiguration.SectionName));
            services.AddScoped<ICardProvider, CardProvider>();

            services.Configure<BankConfiguration>(configuration.GetSection(BankConfiguration.SectionName));
            services.AddScoped<IBankProvider, BankProvider>();

            // ── Refit ─────────────────────────────────────────────────────────────
            services.AddRefitClient<IMpesaClient>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var config = sp.GetRequiredService<IOptions<MpesaConfiguration>>().Value;
                    client.BaseAddress = new Uri(config.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                });

            // ── Webhooks & MongoDB ────────────────────────────────────────────────
            services.Configure<MongoWebhookConfiguration>(configuration.GetSection(MongoWebhookConfiguration.SectionName));
            services.AddScoped<IMongoWebhookService, MongoWebhookService>();

            // ── Idempotency ───────────────────────────────────────────────────────
            services.AddScoped<IIdempotencyService, SqlIdempotencyService>();

            // ── Services ──────────────────────────────────────────────────────────
            services.AddScoped<IDateTimeService, DateTimeService>();
            services.AddScoped<IOutboxPublisher, OutboxPublisher>();
            services.AddScoped<ILedgerService, LedgerService>();

            // ── Quartz Jobs ───────────────────────────────────────────────────────
            services.AddQuartz();
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
            services.AddScoped<JobScheduler>();

            return services;
        }
    }
}