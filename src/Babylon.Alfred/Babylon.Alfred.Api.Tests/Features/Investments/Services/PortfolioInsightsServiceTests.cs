using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Analyzers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class PortfolioInsightsServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly PortfolioInsightsService sut;

    public PortfolioInsightsServiceTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Setup default mocks for analyzer dependencies
        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>());

        // Register analyzers explicitly
        var analyzers = new List<IPortfolioAnalyzer>
        {
            new RiskAnalyzer(),
            new IncomeAnalyzer(),
            new EfficiencyAnalyzer(),
            autoMocker.CreateInstance<TrendAnalyzer>() // Needs IMarketPriceService
        };
        autoMocker.Use<IEnumerable<IPortfolioAnalyzer>>(analyzers);

        sut = autoMocker.CreateInstance<PortfolioInsightsService>();
    }

    [Fact]
    public async Task GetTopInsightsAsync_WithNoPortfolio_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyPortfolio = new PortfolioResponse { Positions = [], TotalInvested = 0 };
        var emptyHistory = new List<Transaction>();

        autoMocker.GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(emptyPortfolio);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(emptyHistory);

        // Act
        var result = await sut.GetTopInsightsAsync(userId, 3);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopInsightsAsync_ShouldRunAllAnalyzers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var portfolio = new PortfolioResponse
        {
            Positions = fixture.Build<PortfolioPositionDto>()
                .With(p => p.TotalInvested, 1000m)
                .CreateMany(2)
                .ToList(),
            TotalInvested = 2000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolio);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(history);

        // Act
        var result = await sut.GetTopInsightsAsync(userId, 3);

        // Assert
        // Should have at least one insight (Low Diversification from RiskAnalyzer)
        result.Should().NotBeEmpty();
        result.Should().Contain(i => i.Title == "Low Diversification");
    }

    [Fact]
    public async Task GetTopInsightsAsync_ShouldSortBySeverity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var concentratedPosition = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.SecurityName, "Nvidia")
            .With(p => p.TotalInvested, 4500m) // 45% - Critical
            .Create();
        var portfolio = new PortfolioResponse
        {
            Positions = [concentratedPosition],
            TotalInvested = 10000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolio);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(history);

        // Act
        var result = (await sut.GetTopInsightsAsync(userId, 5)).ToList();

        // Assert
        result.Should().NotBeEmpty();
        // Critical insights should come first
        var criticalInsights = result.Where(i => i.Severity == InsightSeverity.Critical).ToList();
        var warningInsights = result.Where(i => i.Severity == InsightSeverity.Warning).ToList();
        var infoInsights = result.Where(i => i.Severity == InsightSeverity.Info).ToList();

        if (criticalInsights.Any() && warningInsights.Any())
        {
            result.IndexOf(criticalInsights.First()).Should().BeLessThan(result.IndexOf(warningInsights.First()));
        }
        if (warningInsights.Any() && infoInsights.Any())
        {
            result.IndexOf(warningInsights.First()).Should().BeLessThan(result.IndexOf(infoInsights.First()));
        }
    }

    [Fact]
    public async Task GetTopInsightsAsync_ShouldLimitResultsByCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var positions = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 2500m) // 25% each - triggers concentration risk
            .CreateMany(4)
            .ToList();
        var portfolio = new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = 10000m
        };
        var history = new List<Transaction>();

        autoMocker.GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolio);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(history);

        // Act
        var result = await sut.GetTopInsightsAsync(userId, 2);

        // Assert
        result.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetTopInsightsAsync_ShouldPrioritizeRiskInsights()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var lastYear = now.Year - 1;

        // Create portfolio with concentration risk
        var concentratedPosition = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.TotalInvested, 3000m) // 30% - Warning
            .Create();
        var portfolio = new PortfolioResponse
        {
            Positions = [concentratedPosition],
            TotalInvested = 10000m
        };

        // Create dividend history for income insight
        var security = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();
        var dividend = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Date, new DateTime(lastYear, currentMonth, 15))
            .With(t => t.SharePrice, 0.25m)
            .With(t => t.SharesQuantity, 100m)
            .Create();
        var history = new List<Transaction> { dividend };

        autoMocker.GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolio);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(history);

        // Act
        var result = (await sut.GetTopInsightsAsync(userId, 5)).ToList();

        // Assert
        result.Should().NotBeEmpty();
        // Risk insights should be prioritized over Income insights
        var riskInsights = result.Where(i => i.Category == InsightCategory.Risk).ToList();
        var incomeInsights = result.Where(i => i.Category == InsightCategory.Income).ToList();

        if (riskInsights.Any() && incomeInsights.Any())
        {
            result.IndexOf(riskInsights.First()).Should().BeLessThan(result.IndexOf(incomeInsights.First()));
        }
    }
}

