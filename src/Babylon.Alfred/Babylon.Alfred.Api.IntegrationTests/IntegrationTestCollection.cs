namespace Babylon.Alfred.Api.IntegrationTests;

/// <summary>One Postgres container + one Api host, shared across every test in this assembly.</summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<BabylonApiFactory>
{
    public const string Name = "Integration";
}
