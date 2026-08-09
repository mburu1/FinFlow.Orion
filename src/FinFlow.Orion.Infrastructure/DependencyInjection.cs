using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Infrastructure.Auth;
using FinFlow.Orion.Infrastructure.Idempotency;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddDbContext<LedgerDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            // ── Unit of Work & Repositories ───────────────────────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILedgerRepository, LedgerRepository>();
            services.AddScoped<IJournalEntryRepository, JournalEntryRepository>(); // ← ADDED THIS
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
            services.AddScoped<IWebhookRepository, WebhookRepository>();
            services.AddScoped<IPaymentSagaStateRepository, PaymentSagaStateRepository>();
            services.AddScoped<IOutboxService, OutboxService>();
            // Inside AddInfrastructure method:
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<FinFlow.Orion.Application.Common.Interfaces.IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.Configure<JwtConfiguration>(configuration.GetSection(JwtConfiguration.SectionName));

            // ── Providers ─────────────────────────────────────────────────────────
            services.Configure<MpesaConfiguration>(configuration.GetSection(MpesaConfiguration.SectionName));
            services.AddScoped<IMpesaProvider, MpesaProvider>();

            services.Configure<CardConfiguration>(configuration.GetSection(CardConfiguration.SectionName));
            services.AddScoped<ICardProvider, CardProvider>();

            services.Configure<BankConfiguration>(configuration.GetSection(BankConfiguration.SectionName));
            services.AddScoped<IBankProvider, BankProvider>();

            services.AddScoped<IPaymentProviderDispatcher, Providers.PaymentProviderDispatcher>();

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
            services.TryAddScoped<ILedgerService, LedgerService>();

            // Quartz job scheduling (Outbox processor, Reconciliation) is owned
            // solely by FinFlow.Orion.Workers — see Workers/Program.cs. Api and
            // Webhooks only write outbox rows via ApplicationDbContext.SaveChangesAsync;
            // they don't run the scheduler.

            return services;
        }
    }
}