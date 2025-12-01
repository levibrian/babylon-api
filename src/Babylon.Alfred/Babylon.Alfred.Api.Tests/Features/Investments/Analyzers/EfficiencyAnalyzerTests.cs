using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Analyzers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Analyzers;

public class EfficiencyAnalyzerTests
{
    private readonly Fixture fixture = new();
    private readonly EfficiencyAnalyzer sut;

    public EfficiencyAnalyzerTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = new EfficiencyAnalyzer();
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
    public async Task AnalyzeAsync_WithPortfolio_ShouldReturnNoInsights_AsPlaceholder()
    {
        // Arrange
        var positions = fixture.Build<PortfolioPositionDto>()
            .With(p => p.TotalInvested, 1000m)
            .CreateMany(3)
            .ToList();
        var portfolio = new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = 3000m
        };
        var history = new List<Transaction>();

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        // Currently returns empty as checks are placeholders for future implementation
        result.Should().BeEmpty();
    }
}

