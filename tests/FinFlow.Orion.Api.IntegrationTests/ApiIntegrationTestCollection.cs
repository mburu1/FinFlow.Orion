using Xunit;

namespace FinFlow.Orion.Api.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "ApiIntegration";
}
