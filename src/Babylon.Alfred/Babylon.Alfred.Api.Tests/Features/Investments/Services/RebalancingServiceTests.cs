using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class RebalancingServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly RebalancingService sut;

    public RebalancingServiceTests()
    {
        sut = autoMocker.CreateInstance<RebalancingService>();
    }

    [Fact]
    public async Task GetRebalancingActions_ShouldUseMarketValueFromPortfolio()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var portfolio = new PortfolioResponse
        {
            TotalInvested = 1000m,
            TotalMarketValue = 1200m, // Market value is different from invested
            Positions = new List<PortfolioPositionDto>
            {
                new()
                {
                    Ticker = "NVDA",
                    CurrentMarketValue = 720m, // 60% of 1200
                    TotalInvested = 500m,
                    CurrentAllocationPercentage = 60m,
                    TargetAllocationPercentage = 50m
                },
                new()
                {
                    Ticker = "AAPL",
                    CurrentMarketValue = 480m, // 40% of 1200
                    TotalInvested = 500m,
                    CurrentAllocationPercentage = 40m,
                    TargetAllocationPercentage = 50m
                }
            }
        };

        autoMocker.GetMock<IPortfolioService>().Setup(x => x.GetPortfolio(userId)).ReturnsAsync(portfolio);

        // Act
        var result = await sut.GetRebalancingActionsAsync(userId);

        // Assert
        // Total Portfolio Value should be 1200 (Market Value)
        // NVDA Difference: (50% - 60%) of 1200 = -10% of 1200 = -120
        // AAPL Difference: (50% - 40%) of 1200 = +10% of 1200 = +120

        result.TotalPortfolioValue.Should().Be(1200m);
        var nvdaAction = result.Actions.First(a => a.Ticker == "NVDA");
        var aaplAction = result.Actions.First(a => a.Ticker == "AAPL");

        nvdaAction.DifferenceValue.Should().Be(-120m);
        aaplAction.DifferenceValue.Should().Be(120m);
    }
}
