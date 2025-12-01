using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Analyzers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Analyzers;

public class TrendAnalyzerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly TrendAnalyzer sut;

    public TrendAnalyzerTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<TrendAnalyzer>();
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyPortfolio_ShouldReturnNoInsights()
    {
        // Arrange
        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 0 };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>());

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithMomentumWinner_ShouldReturnMomentumAlert()
    {
        // Arrange
        var position = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.SecurityName, "Nvidia")
            .With(p => p.AverageSharePrice, 100m)
            .With(p => p.TotalShares, 10m)
            .With(p => p.TotalInvested, 1000m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [position],
            TotalInvested = 1000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "NVDA", 130m } }); // 30% gain

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be(InsightCategory.Trend);
        result[0].Title.Should().Be("Momentum Alert");
        result[0].Message.Should().Contain("Nvidia");
        result[0].Message.Should().Contain("30.0%");
        result[0].RelatedTicker.Should().Be("NVDA");
        result[0].Severity.Should().Be(InsightSeverity.Info);
        result[0].Metadata["percentageChange"].Should().Be(30m);
        result[0].Metadata["unrealizedGain"].Should().Be(300m); // (130 - 100) * 10
    }

    [Fact]
    public async Task AnalyzeAsync_WithExtremeMomentumWinner_ShouldReturnWarningSeverity()
    {
        // Arrange
        var position = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.SecurityName, "Nvidia")
            .With(p => p.AverageSharePrice, 100m)
            .With(p => p.TotalShares, 10m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [position],
            TotalInvested = 1000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "NVDA", 160m } }); // 60% gain

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Severity.Should().Be(InsightSeverity.Warning);
        result[0].Metadata["percentageChange"].Should().Be(60m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithDrawdownLoser_ShouldReturnDrawdownAlert()
    {
        // Arrange
        var position = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "BTC")
            .With(p => p.SecurityName, "Bitcoin")
            .With(p => p.AverageSharePrice, 50000m)
            .With(p => p.TotalShares, 0.1m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [position],
            TotalInvested = 5000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "BTC", 40000m } }); // 20% loss

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be(InsightCategory.Risk);
        result[0].Title.Should().Be("Drawdown Alert");
        result[0].Message.Should().Contain("Bitcoin");
        result[0].Message.Should().Contain("20.0%");
        result[0].RelatedTicker.Should().Be("BTC");
        result[0].Severity.Should().Be(InsightSeverity.Warning);
        result[0].Metadata["percentageChange"].Should().Be(-20m);
        result[0].Metadata["unrealizedLoss"].Should().Be(1000m); // (50000 - 40000) * 0.1
    }

    [Fact]
    public async Task AnalyzeAsync_WithExtremeDrawdownLoser_ShouldReturnCriticalSeverity()
    {
        // Arrange
        var position = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "BTC")
            .With(p => p.SecurityName, "Bitcoin")
            .With(p => p.AverageSharePrice, 50000m)
            .With(p => p.TotalShares, 0.1m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [position],
            TotalInvested = 5000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "BTC", 30000m } }); // 40% loss

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Severity.Should().Be(InsightSeverity.Critical);
        result[0].Metadata["percentageChange"].Should().Be(-40m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNoPriceChange_ShouldReturnNoInsights()
    {
        // Arrange
        var position = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "AAPL")
            .With(p => p.AverageSharePrice, 150m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [position],
            TotalInvested = 1500m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "AAPL", 150m } }); // No change

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithMissingPriceData_ShouldSkipPosition()
    {
        // Arrange
        var position = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "AAPL")
            .With(p => p.AverageSharePrice, 150m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [position],
            TotalInvested = 1500m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>()); // No price data

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithBothWinnersAndLosers_ShouldReturnBothInsights()
    {
        // Arrange
        var winner = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.AverageSharePrice, 100m)
            .Create();

        var loser = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "BTC")
            .With(p => p.AverageSharePrice, 50000m)
            .Create();

        var portfolio = new PortfolioResponse
        {
            Positions = [winner, loser],
            TotalInvested = 51000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                { "NVDA", 130m }, // 30% gain
                { "BTC", 40000m } // 20% loss
            });

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.Title == "Momentum Alert" && i.RelatedTicker == "NVDA");
        result.Should().Contain(i => i.Title == "Drawdown Alert" && i.RelatedTicker == "BTC");
    }
}

