using FluentAssertions;

namespace Babylon.Alfred.Api.IntegrationTests.Mcp;

// Depends on BabylonApiFactory (unused directly) solely to join the shared "Integration"
// collection — guarantees its connection string + JWT env vars are set before this runs,
// and keeps this test from racing a second Postgres container in a parallel collection.
[Collection(IntegrationTestCollection.Name)]
public class AuthorizePageStaticFilesTests(BabylonApiFactory apiFactory)
{
    [Fact]
    public async Task GetLogo_ReturnsOkWithImageContentType()
    {
        _ = apiFactory;
        using var factory = new McpApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/babylon_logo.png");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should().StartWith("image/");
    }
}
