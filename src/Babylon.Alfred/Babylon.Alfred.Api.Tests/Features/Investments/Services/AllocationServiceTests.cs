using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class AllocationServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly AllocationService sut;

    public AllocationServiceTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<AllocationService>();
    }

    [Fact]
    public async Task GetTargets_WhenUserHasTargets_ShouldReturnTickerAndPercentage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var security = new Security { Id = Guid.NewGuid(), Ticker = "VWCE" };
        autoMocker.GetMock<IAllocationStrategyRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<AllocationStrategy>
            {
                new() { UserId = userId, SecurityId = security.Id, Security = security, TargetPercentage = 60m }
            });

        // Act
        var result = await sut.GetTargets(userId);

        // Assert
        result.Should().ContainSingle(t => t.Ticker == "VWCE" && t.TargetPercentage == 60m);
    }

    [Fact]
    public async Task ReplaceTargets_WithValidRequest_ShouldPersistUserScopedStrategies()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vwce = new Security { Id = Guid.NewGuid(), Ticker = "VWCE" };
        var aapl = new Security { Id = Guid.NewGuid(), Ticker = "AAPL" };
        SetupSecurities(vwce, aapl);
        var request = Request(("vwce", 70m), ("AAPL", 30m));

        // Act
        var result = await sut.ReplaceTargets(userId, request);

        // Assert
        result.Should().HaveCount(2);
        autoMocker.GetMock<IAllocationStrategyRepository>().Verify(x => x.ReplaceForUserAsync(
            userId,
            It.Is<IReadOnlyCollection<AllocationStrategy>>(s =>
                s.Count == 2
                && s.All(a => a.UserId == userId)
                && s.Any(a => a.SecurityId == vwce.Id && a.TargetPercentage == 70m)
                && s.Any(a => a.SecurityId == aapl.Id && a.TargetPercentage == 30m))),
            Times.Once);
    }

    [Fact]
    public async Task ReplaceTargets_WhenTotalIsExactly100_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities(new Security { Id = Guid.NewGuid(), Ticker = "VWCE" }, new Security { Id = Guid.NewGuid(), Ticker = "AAPL" });
        var request = Request(("VWCE", 99.5m), ("AAPL", 0.5m));

        // Act
        Func<Task> act = async () => await sut.ReplaceTargets(userId, request);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReplaceTargets_WhenTotalExceeds100_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities(new Security { Id = Guid.NewGuid(), Ticker = "VWCE" }, new Security { Id = Guid.NewGuid(), Ticker = "AAPL" });
        var request = Request(("VWCE", 70m), ("AAPL", 30.0001m));

        // Act
        Func<Task> act = async () => await sut.ReplaceTargets(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*100*");
        VerifyNothingPersisted();
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(100.0001)]
    public async Task ReplaceTargets_WhenTargetOutOfRange_ShouldThrowArgumentException(decimal target)
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities(new Security { Id = Guid.NewGuid(), Ticker = "VWCE" });
        var request = Request(("VWCE", target));

        // Act
        Func<Task> act = async () => await sut.ReplaceTargets(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*between 0 and 100*");
        VerifyNothingPersisted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task ReplaceTargets_WhenTargetOnRangeBoundary_ShouldSucceed(decimal target)
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities(new Security { Id = Guid.NewGuid(), Ticker = "VWCE" });
        var request = Request(("VWCE", target));

        // Act
        Func<Task> act = async () => await sut.ReplaceTargets(userId, request);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReplaceTargets_WhenTickerUnknown_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities(new Security { Id = Guid.NewGuid(), Ticker = "VWCE" });
        var request = Request(("VWCE", 50m), ("NOPE", 10m));

        // Act
        Func<Task> act = async () => await sut.ReplaceTargets(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*NOPE*");
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ReplaceTargets_WhenTickerDuplicated_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities(new Security { Id = Guid.NewGuid(), Ticker = "VWCE" });
        var request = Request(("VWCE", 50m), ("vwce", 10m));

        // Act
        Func<Task> act = async () => await sut.ReplaceTargets(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*VWCE*");
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ReplaceTargets_WithEmptyList_ShouldClearUserTargets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupSecurities();

        // Act
        var result = await sut.ReplaceTargets(userId, new UpdateAllocationTargetsRequest());

        // Assert
        result.Should().BeEmpty();
        autoMocker.GetMock<IAllocationStrategyRepository>().Verify(x => x.ReplaceForUserAsync(
            userId, It.Is<IReadOnlyCollection<AllocationStrategy>>(s => s.Count == 0)), Times.Once);
    }

    private void SetupSecurities(params Security[] securities)
    {
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> tickers) => securities
                .Where(s => tickers.Contains(s.Ticker))
                .ToDictionary(s => s.Ticker, s => s));
    }

    private void VerifyNothingPersisted()
    {
        autoMocker.GetMock<IAllocationStrategyRepository>().Verify(x => x.ReplaceForUserAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<AllocationStrategy>>()), Times.Never);
    }

    private static UpdateAllocationTargetsRequest Request(params (string Ticker, decimal Target)[] targets) => new()
    {
        Targets = targets
            .Select(t => new AllocationTargetRequest { Ticker = t.Ticker, TargetPercentage = t.Target })
            .ToList()
    };
}
