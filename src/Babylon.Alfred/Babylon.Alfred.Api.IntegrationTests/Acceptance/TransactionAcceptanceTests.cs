using System.Net;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.IntegrationTests.Acceptance;

[Collection(IntegrationTestCollection.Name)]
public class TransactionAcceptanceTests(BabylonApiFactory factory)
{
    [Fact]
    public async Task CreateBuyTransaction_ThenGetPortfolio_ShowsThePosition()
    {
        // Arrange
        var client = factory.CreateClient();
        await client.RegisterAndAuthenticateAsync();
        var ticker = $"TST{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        await factory.SeedSecurityAsync(ticker, "Test Security Inc.");

        var request = new CreateTransactionRequest
        {
            Ticker = ticker,
            TransactionType = TransactionType.Buy,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            SharesQuantity = 10m,
            SharePrice = 100m,
            Fees = 5m
        };

        // Act
        var createResponse = await client.PostJsonAsync("/api/v1/transactions", request);
        var portfolioResponse = await client.GetAsync("/api/v1/portfolios");
        var portfolioBody = await portfolioResponse.ReadJsonAsync<ApiResponse<PortfolioResponse>>();

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var position = portfolioBody!.Data!.Positions.Should().ContainSingle(p => p.Ticker == ticker).Subject;
        position.TotalShares.Should().Be(10m);
        position.TotalInvested.Should().Be(1005m); // (10 * 100) + 5 fees
    }
}
