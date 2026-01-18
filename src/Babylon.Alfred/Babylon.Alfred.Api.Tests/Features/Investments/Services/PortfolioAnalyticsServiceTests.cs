using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class PortfolioAnalyticsServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly PortfolioAnalyticsService sut;

    public PortfolioAnalyticsServiceTests()
    {
        sut = autoMocker.CreateInstance<PortfolioAnalyticsService>();
    }

    [Fact]
    public async Task GetDiversificationMetrics_ShouldIncludeCashInHHI()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var portfolio = new PortfolioResponse
        {
            CashAmount = 1000m,
            TotalMarketValue = 2000m,
            Positions = new List<PortfolioPositionDto>
            {
                new()
                {
                    Ticker = "AAPL",
                    CurrentMarketValue = 1000m,
                    TotalInvested = 1000m,
                    CurrentAllocationPercentage = 50m
                }
            }
        };

        autoMocker.GetMock<IPortfolioService>().Setup(x => x.GetPortfolio(userId)).ReturnsAsync(portfolio);

    // Act
    var result = await sut.GetDiversificationMetricsAsync(userId);

    // Assert
    // Total Value = 2000
    // AAPL weight = 0.5 (1000/2000)
    // Cash weight = 0.5 (1000/2000)
    // HHI = 0.5^2 + 0.5^2 = 0.25 + 0.25 = 0.5
    // Diversification Score = (1 - 0.5) * 100 = 50
    // TotalAssets = 1 (AAPL) + 1 (CASH) = 2

    result.HHI.Should().Be(0.5m);
    result.DiversificationScore.Should().Be(50m);
    result.TotalAssets.Should().Be(2);
    result.Top3Concentration.Should().Be(100m); // 50% AAPL + 50% Cash
}

[Fact]
public async Task GetDiversificationMetrics_WithOnlyCash_ShouldReturnMetrics()
{
    // Arrange
    var userId = Guid.NewGuid();
    var portfolio = new PortfolioResponse
    {
        CashAmount = 1000m,
        TotalMarketValue = 1000m,
        Positions = new List<PortfolioPositionDto>()
    };

    autoMocker.GetMock<IPortfolioService>().Setup(x => x.GetPortfolio(userId)).ReturnsAsync(portfolio);

        // Act
        var result = await sut.GetDiversificationMetricsAsync(userId);

        // Assert
        // Total Value = 1000
        // Cash weight = 1.0
        // HHI = 1.0
        // Diversification Score = 0
        // TotalAssets = 1 (CASH)

        result.HHI.Should().Be(1.0m);
        result.DiversificationScore.Should().Be(0m);
        result.TotalAssets.Should().Be(1);
    }
}
