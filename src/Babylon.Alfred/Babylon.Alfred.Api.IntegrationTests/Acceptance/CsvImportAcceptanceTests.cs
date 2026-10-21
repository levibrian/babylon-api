using System.Net;
using Babylon.Alfred.Api.Features.Import.Models.Responses;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.IntegrationTests.Acceptance;

[Collection(IntegrationTestCollection.Name)]
public class CsvImportAcceptanceTests(BabylonApiFactory factory)
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "trade-republic-sample.csv");

    [Fact]
    public async Task ImportTwice_SecondImportAddsZeroRows()
    {
        // Arrange
        var client = factory.CreateClient();
        await client.RegisterAndAuthenticateAsync();
        await factory.SeedSecurityAsync("WORLD", "Fixture World ETF", isin: "XX0000000001");
        await factory.SeedSecurityAsync("DIVCO", "Fixture Dividend Co", isin: "XX0000000002");

        // Act — first import
        var firstResponse = await ImportFixtureAsync(client);
        var firstBody = await firstResponse.ReadJsonAsync<ApiResponse<ImportResult>>();

        // Act — re-import the exact same file
        var secondResponse = await ImportFixtureAsync(client);
        var secondBody = await secondResponse.ReadJsonAsync<ApiResponse<ImportResult>>();

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstBody!.Data!.Imported.Should().Be(3); // Buy + Sell + Dividend; unknown-ISIN row errors, CASH rows skipped
        firstBody.Data.Errors.Should().ContainSingle();

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondBody!.Data!.Imported.Should().Be(0);
        secondBody.Data.SkippedDuplicates.Should().Be(3);
    }

    private static async Task<HttpResponseMessage> ImportFixtureAsync(HttpClient client)
    {
        using var content = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(FixturePath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "trade-republic-sample.csv");

        return await client.PostAsync("/api/v1/imports/trade-republic", content);
    }
}
