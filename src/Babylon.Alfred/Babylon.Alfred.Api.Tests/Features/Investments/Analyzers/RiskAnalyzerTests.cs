using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Analyzers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Analyzers;

public class RiskAnalyzerTests
{
    private readonly Fixture fixture = new();
    private readonly RiskAnalyzer sut;

    public RiskAnalyzerTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = new RiskAnalyzer();
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyPortfolio_ShouldReturnNoInsights()
    {
        // Arrange
        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 0 };
        var history = new List<Transaction>();

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithZeroTotalInvested_ShouldReturnNoInsights()
    {
        // Arrange
        var positions = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 1000m)
            .CreateMany(3)
            .ToList();
        var portfolio = new PortfolioResponse { Positions = positions, TotalInvested = 0 };
        var history = new List<Transaction>();

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithConcentrationRisk_ShouldReturnWarning()
    {
        // Arrange
        var totalInvested = 10000m;
        var concentratedPosition = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.SecurityName, "Nvidia")
            .With(p => p.TotalInvested, 2500m) // 25% allocation
            .Create();
        var otherPosition = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 7500m) // 75% allocation
            .Create();
        var portfolio = new PortfolioResponse
        {
            Positions = [concentratedPosition, otherPosition],
            TotalInvested = totalInvested
        };
        var history = new List<Transaction>();

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        // Should have 2 concentration risk insights (both positions > 20%) + 1 low diversification
        var concentrationInsights = result.Where(r => r.Title == "Concentration Risk").ToList();
        concentrationInsights.Should().HaveCount(2);
        var nvidiaInsight = concentrationInsights.First(r => r.RelatedTicker == "NVDA");
        nvidiaInsight.Category.Should().Be(InsightCategory.Risk);
        nvidiaInsight.Message.Should().Contain("Nvidia");
        nvidiaInsight.Message.Should().Contain("25.0%");
        nvidiaInsight.Severity.Should().Be(InsightSeverity.Warning);
        nvidiaInsight.Metadata["allocationPercentage"].Should().Be(25m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCriticalConcentrationRisk_ShouldReturnCriticalSeverity()
    {
        // Arrange
        var totalInvested = 10000m;
        var concentratedPosition = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.SecurityName, "Nvidia")
            .With(p => p.TotalInvested, 4500m) // 45% allocation
            .Create();
        var portfolio = new PortfolioResponse
        {
            Positions = [concentratedPosition],
            TotalInvested = totalInvested
        };
        var history = new List<Transaction>();

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(2); // Concentration + Low Diversification
        var concentrationInsight = result.First(r => r.Title == "Concentration Risk");
        concentrationInsight.Severity.Should().Be(InsightSeverity.Critical);
        concentrationInsight.Metadata["allocationPercentage"].Should().Be(45m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithLowDiversification_ShouldReturnWarning()
    {
        // Arrange
        var totalInvested = 10000m;
        // Use positions < 20% to avoid concentration risk insights
        var positions = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 3333m) // 33.33% each, but we'll use smaller amounts
            .CreateMany(3)
            .ToList();
        // Adjust to avoid concentration risk (each < 20%)
        positions[0] = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 1500m) // 15%
            .Create();
        positions[1] = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 1500m) // 15%
            .Create();
        positions[2] = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 7000m) // 70% - this will trigger concentration
            .Create();
        var portfolio = new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = totalInvested
        };
        var history = new List<Transaction>();

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        // Should have 1 concentration risk (70% position) + 1 low diversification
        var diversificationInsight = result.FirstOrDefault(r => r.Title == "Low Diversification");
        diversificationInsight.Should().NotBeNull();
        diversificationInsight!.Category.Should().Be(InsightCategory.Risk);
        diversificationInsight.Message.Should().Contain("3 asset");
        diversificationInsight.Severity.Should().Be(InsightSeverity.Info);
        diversificationInsight.Metadata["assetCount"].Should().Be(3);
        diversificationInsight.Metadata["recommendedMinimum"].Should().Be(5);
    }

    [Fact]
    public async Task AnalyzeAsync_WithVeryLowDiversification_ShouldReturnWarningSeverity()
    {
        // Arrange
        var totalInvested = 10000m;
        // Use positions < 20% to avoid concentration risk, but we'll still get concentration for 50% positions
        var positions = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 5000m) // 50% each - will trigger concentration risk
            .CreateMany(2)
            .ToList();
        var portfolio = new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = totalInvested
        };
        var history = new List<Transaction>();

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        // Should have 2 concentration risk insights (both 50%) + 1 low diversification
        var diversificationInsight = result.FirstOrDefault(r => r.Title == "Low Diversification");
        diversificationInsight.Should().NotBeNull();
        diversificationInsight!.Title.Should().Be("Low Diversification");
        diversificationInsight.Severity.Should().Be(InsightSeverity.Warning);
    }

    [Fact]
    public async Task AnalyzeAsync_WithWellDiversifiedPortfolio_ShouldReturnNoDiversificationInsight()
    {
        // Arrange
        var totalInvested = 10000m;
        var positions = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 2000m)
            .CreateMany(5)
            .ToList();
        var portfolio = new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = totalInvested
        };
        var history = new List<Transaction>();

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().NotContain(i => i.Title == "Low Diversification");
    }

    [Fact]
    public async Task AnalyzeAsync_WithMultipleConcentrationRisks_ShouldReturnMultipleInsights()
    {
        // Arrange
        var totalInvested = 10000m;
        var position1 = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "NVDA")
            .With(p => p.TotalInvested, 2500m) // 25%
            .Create();
        var position2 = fixture.Build<PortfolioPositionDto>()
            .With(p => p.Ticker, "AAPL")
            .With(p => p.TotalInvested, 3000m) // 30%
            .Create();
        var position3 = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 4500m) // 45%
            .Create();
        var portfolio = new PortfolioResponse
        {
            Positions = [position1, position2, position3],
            TotalInvested = totalInvested
        };
        var history = new List<Transaction>();

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        var concentrationInsights = result.Where(r => r.Title == "Concentration Risk").ToList();
        concentrationInsights.Should().HaveCount(3);
        concentrationInsights.Should().Contain(i => i.RelatedTicker == "NVDA");
        concentrationInsights.Should().Contain(i => i.RelatedTicker == "AAPL");
    }
}

