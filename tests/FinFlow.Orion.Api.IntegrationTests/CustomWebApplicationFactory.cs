using FinFlow.Orion.Infrastructure.Persistence;
using FinFlow.Orion.Ledger.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Xunit;

namespace FinFlow.Orion.Api.IntegrationTests;

/// <summary>
/// Hosts the real Api pipeline (DI, middleware, controllers) against ephemeral
/// SQL Server and MongoDB containers instead of the dev appsettings.json targets,
/// so these tests exercise the actual wiring end-to-end without touching a real
/// database. Api never talks to RabbitMQ directly (see README), so no broker
/// container is needed for this tier.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder("mongo:7").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _mongoContainer.StartAsync());

        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        // The Card payment path exercises ledger posting synchronously (via the
        // PaymentCompletedEvent handler), so the ledger schema and its seed
        // accounts must exist too — LedgerDbContext is a separate DbContext
        // (same physical database) with its own migration set.
        var ledgerDbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await ledgerDbContext.Database.MigrateAsync();
        ledgerDbContext.LedgerAccounts.AddRange(LedgerAccountSeeds.GetSeedAccounts());
        await ledgerDbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development" — ASP.NET Core auto-inserts a developer exception
        // page ahead of user middleware in that environment, which would swallow
        // exceptions before our own ExceptionHandlingMiddleware (and its
        // ProblemDetails status-code mapping) gets a chance to run.
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                ["MongoDB:ConnectionString"] = _mongoContainer.GetConnectionString(),
                ["Jwt:Key"] = "integration-test-signing-key-32chars-minimum-length",
                ["Jwt:Issuer"] = "FinFlow.Orion.Tests",
                ["Jwt:Audience"] = "FinFlow.Orion.Tests.Clients"
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }
}
