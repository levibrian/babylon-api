using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Analyzers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Analyzers;

public class IncomeAnalyzerTests
{
    private readonly Fixture fixture = new();
    private readonly IncomeAnalyzer sut;

    public IncomeAnalyzerTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = new IncomeAnalyzer();
    }

    [Fact]
    public async Task AnalyzeAsync_WithNoDividendHistory_ShouldReturnNoInsights()
    {
        // Arrange
        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 10000m };
        var history = new List<Transaction>();

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithDividendInCurrentMonthPreviousYear_ShouldReturnInsight()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var lastYear = now.Year - 1;

        var security = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();

        var dividendLastYear = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Date, new DateTime(lastYear, currentMonth, 15))
            .With(t => t.SharePrice, 0.25m) // Dividend per share
            .With(t => t.SharesQuantity, 100m)
            .Create();

        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 10000m };
        var history = new List<Transaction> { dividendLastYear };

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be(InsightCategory.Income);
        result[0].Title.Should().Be("Dividend Season");
        result[0].Message.Should().Contain("Apple Inc.");
        result[0].Message.Should().Contain(now.ToString("MMMM"));
        result[0].RelatedTicker.Should().Be("AAPL");
        result[0].Severity.Should().Be(InsightSeverity.Info);
        result[0].ActionLabel.Should().Be("Log Receipt");
        result[0].Metadata.Should().ContainKey("expectedDate");
        result[0].Metadata.Should().ContainKey("estimatedAmount");
    }

    [Fact]
    public async Task AnalyzeAsync_WithDividendTooFarInFuture_ShouldReturnNoInsight()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var lastYear = now.Year - 1;

        var security = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();

        // Dividend was 60 days ago (outside 30-day window)
        var dividendLastYear = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Date, now.AddDays(-60))
            .With(t => t.SharePrice, 0.25m)
            .With(t => t.SharesQuantity, 100m)
            .Create();

        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 10000m };
        var history = new List<Transaction> { dividendLastYear };

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithMultipleDividends_ShouldCalculateAverage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var lastYear = now.Year - 1;
        var twoYearsAgo = now.Year - 2;

        var security = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();

        var dividend1 = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Date, new DateTime(twoYearsAgo, currentMonth, 15))
            .With(t => t.SharePrice, 0.20m)
            .With(t => t.SharesQuantity, 100m)
            .Create();

        var dividend2 = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Date, new DateTime(lastYear, currentMonth, 15))
            .With(t => t.SharePrice, 0.25m)
            .With(t => t.SharesQuantity, 100m)
            .Create();

        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 10000m };
        var history = new List<Transaction> { dividend1, dividend2 };

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var estimatedAmount = (decimal)result[0].Metadata["estimatedAmount"];
        var averageDividendPerShare = (decimal)result[0].Metadata["dividendPerShare"];
        averageDividendPerShare.Should().Be(0.225m); // Average of 0.20 and 0.25
        estimatedAmount.Should().Be(22.5m); // 0.225 * 100
    }

    [Fact]
    public async Task AnalyzeAsync_WithDividendInDifferentMonth_ShouldReturnNoInsight()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var differentMonth = now.Month == 1 ? 6 : 1; // Different month
        var lastYear = now.Year - 1;

        var security = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();

        var dividendLastYear = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Date, new DateTime(lastYear, differentMonth, 15))
            .With(t => t.SharePrice, 0.25m)
            .With(t => t.SharesQuantity, 100m)
            .Create();

        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 10000m };
        var history = new List<Transaction> { dividendLastYear };

        // Act
        var result = await sut.AnalyzeAsync(portfolio, history);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithMultipleSecurities_ShouldReturnMultipleInsights()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var lastYear = now.Year - 1;

        var security1 = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();

        var security2 = fixture.Build<Security>()
            .With(s => s.Ticker, "MSFT")
            .With(s => s.SecurityName, "Microsoft")
            .Create();

        var dividend1 = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security1)
            .With(t => t.SecurityId, security1.Id)
            .With(t => t.Date, new DateTime(lastYear, currentMonth, 15))
            .With(t => t.SharePrice, 0.25m)
            .With(t => t.SharesQuantity, 100m)
            .Create();

        var dividend2 = fixture.Build<Transaction>()
            .With(t => t.TransactionType, TransactionType.Dividend)
            .With(t => t.Security, security2)
            .With(t => t.SecurityId, security2.Id)
            .With(t => t.Date, new DateTime(lastYear, currentMonth, 20))
            .With(t => t.SharePrice, 0.75m)
            .With(t => t.SharesQuantity, 50m)
            .Create();

        var portfolio = new PortfolioResponse { Positions = [], TotalInvested = 10000m };
        var history = new List<Transaction> { dividend1, dividend2 };

        // Act
        var result = (await sut.AnalyzeAsync(portfolio, history)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.RelatedTicker == "AAPL");
        result.Should().Contain(i => i.RelatedTicker == "MSFT");
    }
}

