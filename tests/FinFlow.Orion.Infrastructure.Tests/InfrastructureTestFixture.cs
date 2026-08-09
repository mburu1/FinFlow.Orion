using FinFlow.Orion.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Xunit;

namespace FinFlow.Orion.Infrastructure.Tests;

/// <summary>
/// Spins up real SQL Server and MongoDB containers once per test collection and
/// applies ApplicationDbContext's migrations, so repository/service tests exercise
/// the actual EF mappings and Mongo driver — not an in-memory substitute. Testcontainers
/// assigns free host ports automatically, so this never collides with anything else
/// running locally.
/// </summary>
public sealed class InfrastructureTestFixture : IAsyncLifetime
{
    private MsSqlContainer _sqlContainer = null!;
    private MongoDbContainer _mongoContainer = null!;

    public string MongoConnectionString => _mongoContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        _mongoContainer = new MongoDbBuilder("mongo:7").Build();

        await Task.WhenAll(_sqlContainer.StartAsync(), _mongoContainer.StartAsync());

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        // Domain-event dispatch isn't under test here — a no-op mediator keeps
        // SaveChangesAsync working without pulling in the full Application pipeline.
        return new ApplicationDbContext(options, Substitute.For<IMediator>());
    }

    public IMongoDatabase CreateMongoDatabase(string databaseName = "FinFlowOrionTests")
        => new MongoClient(MongoConnectionString).GetDatabase(databaseName);
}

[CollectionDefinition(Name)]
public sealed class InfrastructureTestCollection : ICollectionFixture<InfrastructureTestFixture>
{
    public const string Name = "Infrastructure";
}
