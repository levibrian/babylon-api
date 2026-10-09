using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class PortfolioServiceRebalancingTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly PortfolioService sut;
    private readonly Guid userId = Guid.NewGuid();
    private readonly Security aapl = new() { Id = Guid.NewGuid(), Ticker = "AAPL", SecurityName = "Apple" };
    private readonly Security msft = new() { Id = Guid.NewGuid(), Ticker = "MSFT", SecurityName = "Microsoft" };

    // AAPL = 10 x 100 = 1000, MSFT = 5 x 100 = 500, cash = 500 => total portfolio 2000.
    // AAPL current allocation = 50%, MSFT = 25%.
    public PortfolioServiceRebalancingTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<PortfolioService>();

        var transactions = new List<Transaction> { Buy(aapl, 10m), Buy(msft, 5m) };
        autoMocker.GetMock<ITransactionRepository>().Setup(x => x.GetOpenPositionsByUser(userId)).ReturnsAsync(transactions);
        autoMocker.GetMock<ITransactionRepository>().Setup(x => x.GetAllByUser(userId)).ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Security> { aapl, msft });
        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "AAPL", 100m }, { "MSFT", 100m } });
        autoMocker.GetMock<ICashBalanceService>().Setup(x => x.GetBalanceAsync(userId)).ReturnsAsync(500m);
        autoMocker.GetMock<IAllocationStrategyRepository>()
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<AllocationStrategy>());
    }

    [Fact]
    public async Task GetPortfolio_WithTarget_ShouldPopulateTargetDeviationAmountAndStatus()
    {
        // Arrange
        SetupTargets((aapl, 60m));

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        var position = Position(result, "AAPL");
        position.TargetAllocationPercentage.Should().Be(60m);
        position.AllocationDeviation.Should().Be(-10m);
        position.RebalancingAmount.Should().Be(200m);
        position.RebalancingStatus.Should().Be(RebalancingStatus.Underweight);
    }

    [Fact]
    public async Task GetPortfolio_WhenOverTarget_ShouldReturnNegativeSellAmount()
    {
        // Arrange
        SetupTargets((aapl, 40m));

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        var position = Position(result, "AAPL");
        position.RebalancingAmount.Should().Be(-200m);
        position.RebalancingStatus.Should().Be(RebalancingStatus.Overweight);
    }

    [Theory]
    [InlineData(49.5, RebalancingStatus.Balanced)]
    [InlineData(50.5, RebalancingStatus.Balanced)]
    [InlineData(49.49, RebalancingStatus.Overweight)]
    [InlineData(50.51, RebalancingStatus.Underweight)]
    public async Task GetPortfolio_AtThresholdBoundary_ShouldDetermineStatus(decimal target, RebalancingStatus expected)
    {
        // Arrange
        SetupTargets((aapl, target));

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        Position(result, "AAPL").RebalancingStatus.Should().Be(expected);
    }

    [Fact]
    public async Task GetPortfolio_RebalancingBasis_ShouldIncludeCash()
    {
        // Arrange
        SetupTargets((aapl, 50m));

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        var position = Position(result, "AAPL");
        position.RebalancingAmount.Should().Be(0m);
        position.AllocationDeviation.Should().Be(0m);
        position.RebalancingStatus.Should().Be(RebalancingStatus.Balanced);
    }

    [Fact]
    public async Task GetPortfolio_WhenPositionHasNoTarget_ShouldLeaveRebalancingFieldsNull()
    {
        // Arrange
        SetupTargets((aapl, 60m));

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        var position = Position(result, "MSFT");
        position.TargetAllocationPercentage.Should().BeNull();
        position.AllocationDeviation.Should().BeNull();
        position.RebalancingAmount.Should().BeNull();
        position.RebalancingStatus.Should().BeNull();
    }

    [Fact]
    public async Task GetPortfolio_ShouldOnlyReadTargetsForAuthenticatedUser()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        autoMocker.GetMock<IAllocationStrategyRepository>()
            .Setup(x => x.GetByUserIdAsync(otherUserId))
            .ReturnsAsync(new List<AllocationStrategy> { Target(aapl, 60m, otherUserId) });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        Position(result, "AAPL").TargetAllocationPercentage.Should().BeNull();
        autoMocker.GetMock<IAllocationStrategyRepository>().Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        autoMocker.GetMock<IAllocationStrategyRepository>().Verify(x => x.GetByUserIdAsync(otherUserId), Times.Never);
    }

    private void SetupTargets(params (Security Security, decimal Target)[] targets)
    {
        autoMocker.GetMock<IAllocationStrategyRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(targets.Select(t => Target(t.Security, t.Target, userId)).ToList());
    }

    private static AllocationStrategy Target(Security security, decimal target, Guid ownerId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = ownerId,
        SecurityId = security.Id,
        Security = security,
        TargetPercentage = target
    };

    private Transaction Buy(Security security, decimal shares) => fixture.Build<Transaction>()
        .With(t => t.UserId, userId)
        .With(t => t.SecurityId, security.Id)
        .With(t => t.TransactionType, TransactionType.Buy)
        .With(t => t.SharesQuantity, shares)
        .With(t => t.SharePrice, 100m)
        .With(t => t.Fees, 0m)
        .With(t => t.Tax, 0m)
        .Create();

    private static PortfolioPositionDto Position(PortfolioResponse result, string ticker)
        => result.Positions.Single(p => p.Ticker == ticker);
}
