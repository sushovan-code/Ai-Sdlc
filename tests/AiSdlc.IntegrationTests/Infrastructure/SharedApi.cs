namespace AiSdlc.IntegrationTests.Infrastructure;

/// <summary>One API instance shared by all integration tests; tests isolate themselves with unique accounts.</summary>
[CollectionDefinition(Name)]
public sealed class SharedApi : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
