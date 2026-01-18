using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class CashBalanceServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly CashBalanceService sut;

    public CashBalanceServiceTests()
    {
        sut = autoMocker.CreateInstance<CashBalanceService>();
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnAmountFromRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cashBalance = new CashBalance { UserId = userId, Amount = 1000.50m };
        autoMocker.GetMock<ICashBalanceRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(cashBalance);

        // Act
        var result = await sut.GetBalanceAsync(userId);

        // Assert
        result.Should().Be(1000.50m);
    }

    [Fact]
    public async Task GetBalanceAsync_WhenNoBalanceExists_ShouldReturnZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        autoMocker.GetMock<ICashBalanceRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((CashBalance?)null);

        // Act
        var result = await sut.GetBalanceAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task UpdateManualBalanceAsync_ShouldUpdateRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newAmount = 500m;

        // Act
        await sut.UpdateManualBalanceAsync(userId, newAmount);

        // Assert
        autoMocker.GetMock<ICashBalanceRepository>().Verify(x => x.AddOrUpdateAsync(It.Is<CashBalance>(c =>
            c.UserId == userId &&
            c.Amount == newAmount &&
            c.LastUpdatedSource == CashUpdateSource.Manual)), Times.Once);
    }

    [Fact]
    public async Task UpdateManualBalanceAsync_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newAmount = -100m;

        // Act
        Func<Task> act = async () => await sut.UpdateManualBalanceAsync(userId, newAmount);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Theory]
    [InlineData(TransactionType.Buy, 1000, 200, 800)]
    [InlineData(TransactionType.Buy, 100, 200, 0)] // Overdraft sets to 0
    [InlineData(TransactionType.Sell, 1000, 200, 1200)]
    [InlineData(TransactionType.Dividend, 1000, 50, 1050)]
    public async Task ProcessTransactionAsync_ShouldUpdateBalanceCorrectly(TransactionType type, decimal initial, decimal amount, decimal expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        autoMocker.GetMock<ICashBalanceRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new CashBalance { UserId = userId, Amount = initial });

        // Act
        await sut.ProcessTransactionAsync(userId, type, amount);

        // Assert
        autoMocker.GetMock<ICashBalanceRepository>().Verify(x => x.AddOrUpdateAsync(It.Is<CashBalance>(c =>
            c.UserId == userId &&
            c.Amount == expected &&
            c.LastUpdatedSource == CashUpdateSource.Transaction)), Times.Once);
    }

    [Theory]
    [InlineData(TransactionType.Buy, 1000, 200, 1200)]
    [InlineData(TransactionType.Sell, 1000, 200, 800)]
    [InlineData(TransactionType.Sell, 100, 200, 0)] // Cannot go below 0 when reverting sell
    [InlineData(TransactionType.Dividend, 1000, 50, 950)]
    public async Task RevertTransactionAsync_ShouldRevertBalanceCorrectly(TransactionType type, decimal initial, decimal amount, decimal expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        autoMocker.GetMock<ICashBalanceRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new CashBalance { UserId = userId, Amount = initial });

        // Act
        await sut.RevertTransactionAsync(userId, type, amount);

        // Assert
        autoMocker.GetMock<ICashBalanceRepository>().Verify(x => x.AddOrUpdateAsync(It.Is<CashBalance>(c =>
            c.UserId == userId &&
            c.Amount == expected &&
            c.LastUpdatedSource == CashUpdateSource.Transaction)), Times.Once);
    }
}
