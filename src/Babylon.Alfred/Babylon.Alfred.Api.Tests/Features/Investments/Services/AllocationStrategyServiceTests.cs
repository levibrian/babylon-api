using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class AllocationStrategyServiceTests
{
    private readonly Fixture _fixture = new();
    private readonly AutoMocker _autoMocker = new();
    private readonly AllocationStrategyService _sut;

    public AllocationStrategyServiceTests()
    {
        _sut = _autoMocker.CreateInstance<AllocationStrategyService>();
    }

    [Fact]
    public async Task GetTargetAllocationsAsync_ShouldReturnAllocationsWithSecurityDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock
        };
        var strategies = new List<AllocationStrategy>
        {
            new()
            {
                UserId = userId,
                SecurityId = security.Id,
                Security = security,
                TargetPercentage = 50m,
                IsEnabledForWeekly = true
            }
        };

        _autoMocker.GetMock<IAllocationStrategyRepository>()
            .Setup(x => x.GetAllocationStrategiesByUserIdAsync(userId))
            .ReturnsAsync(strategies);

        // Act
        var result = await _sut.GetTargetAllocationsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Ticker.Should().Be("AAPL");
        result[0].SecurityName.Should().Be("Apple Inc.");
        result[0].SecurityType.Should().Be("Stock");
        result[0].TargetPercentage.Should().Be(50m);
        result[0].IsEnabledForWeekly.Should().BeTrue();
    }

    [Fact]
    public async Task SetAllocationStrategyAsync_ShouldWorkWithValidAllocations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticker = "MSFT";
        var security = new Security { Id = Guid.NewGuid(), Ticker = ticker };
        var allocations = new List<AllocationStrategyDto>
        {
            new() { Ticker = ticker, TargetPercentage = 100m }
        };

        _autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, Security> { { ticker, security } });

        // Act
        await _sut.SetAllocationStrategyAsync(userId, allocations);

        // Assert
        _autoMocker.GetMock<IAllocationStrategyRepository>()
            .Verify(x => x.SetAllocationStrategyAsync(userId, It.Is<List<AllocationStrategy>>(l =>
                l.Count == 1 && l[0].SecurityId == security.Id)), Times.Once);
    }
}
