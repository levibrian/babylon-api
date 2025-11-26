using AutoFixture;
using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;
using Babylon.Alfred.Api.Features.RecurringSchedules.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.RecurringSchedules.Services;

public class RecurringScheduleServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly RecurringScheduleService sut;

    public RecurringScheduleServiceTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<RecurringScheduleService>();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithNewSecurity_ShouldCreateSecurityAndSchedule()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = fixture.Build<CreateRecurringScheduleRequest>()
            .With(r => r.Ticker, "AAPL")
            .With(r => r.SecurityName, "Apple Inc.")
            .With(r => r.Platform, "Binance")
            .With(r => r.TargetAmount, 100m)
            .Create();

        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker,
            SecurityName = request.SecurityName,
            SecurityType = SecurityType.Stock
        };

        var schedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SecurityId = security.Id,
            Platform = request.Platform,
            TargetAmount = request.TargetAmount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Security = security
        };

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync((Security?)null);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.Is<Security>(s => s.Ticker == request.Ticker)))
            .ReturnsAsync(security);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetByUserIdAndSecurityIdAsync(userId, security.Id))
            .ReturnsAsync((RecurringSchedule?)null);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.AddAsync(It.IsAny<RecurringSchedule>()))
            .ReturnsAsync(schedule);

        // Act
        var result = await sut.CreateOrUpdateAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL");
        result.SecurityName.Should().Be("Apple Inc.");
        result.Platform.Should().Be("Binance");
        result.TargetAmount.Should().Be(100m);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(request.Ticker), Times.Once);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.AddOrUpdateAsync(It.Is<Security>(s => s.Ticker == request.Ticker && s.SecurityType == SecurityType.Stock)), Times.Once);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.AddAsync(It.IsAny<RecurringSchedule>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithExistingSecurity_ShouldUseExistingSecurity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = fixture.Build<CreateRecurringScheduleRequest>()
            .With(r => r.Ticker, "AAPL")
            .With(r => r.SecurityName, "Apple Inc.")
            .With(r => r.Platform, "Binance")
            .With(r => r.TargetAmount, 100m)
            .Create();

        var existingSecurity = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker,
            SecurityName = "Apple Inc.",
            SecurityType = SecurityType.Stock
        };

        var schedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SecurityId = existingSecurity.Id,
            Platform = request.Platform,
            TargetAmount = request.TargetAmount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Security = existingSecurity
        };

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(existingSecurity);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetByUserIdAndSecurityIdAsync(userId, existingSecurity.Id))
            .ReturnsAsync((RecurringSchedule?)null);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.AddAsync(It.IsAny<RecurringSchedule>()))
            .ReturnsAsync(schedule);

        // Act
        var result = await sut.CreateOrUpdateAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(request.Ticker), Times.Once);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.AddOrUpdateAsync(It.IsAny<Security>()), Times.Never);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.AddAsync(It.IsAny<RecurringSchedule>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithExistingSchedule_ShouldReactivateAndUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = fixture.Build<CreateRecurringScheduleRequest>()
            .With(r => r.Ticker, "AAPL")
            .With(r => r.SecurityName, "Apple Inc.")
            .With(r => r.Platform, "Trade Republic")
            .With(r => r.TargetAmount, 200m)
            .Create();

        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker,
            SecurityName = request.SecurityName,
            SecurityType = SecurityType.Stock
        };

        var existingSchedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SecurityId = security.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Security = security
        };

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(security);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetByUserIdAndSecurityIdAsync(userId, security.Id))
            .ReturnsAsync(existingSchedule);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.UpdateAsync(It.IsAny<RecurringSchedule>()))
            .ReturnsAsync((RecurringSchedule s) => s);

        // Act
        var result = await sut.CreateOrUpdateAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("Trade Republic");
        result.TargetAmount.Should().Be(200m);
        existingSchedule.IsActive.Should().BeTrue();
        existingSchedule.Platform.Should().Be("Trade Republic");
        existingSchedule.TargetAmount.Should().Be(200m);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.UpdateAsync(existingSchedule), Times.Once);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.AddAsync(It.IsAny<RecurringSchedule>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithNullUserId_ShouldUseRootUserId()
    {
        // Arrange
        var request = fixture.Build<CreateRecurringScheduleRequest>()
            .With(r => r.Ticker, "AAPL")
            .With(r => r.SecurityName, "Apple Inc.")
            .Create();

        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker,
            SecurityName = request.SecurityName,
            SecurityType = SecurityType.Stock
        };

        var schedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = Constants.User.RootUserId,
            SecurityId = security.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Security = security
        };

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(security);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetByUserIdAndSecurityIdAsync(Constants.User.RootUserId, security.Id))
            .ReturnsAsync((RecurringSchedule?)null);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.AddAsync(It.IsAny<RecurringSchedule>()))
            .ReturnsAsync(schedule);

        // Act
        var result = await sut.CreateOrUpdateAsync(null, request);

        // Assert
        result.Should().NotBeNull();
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(
            x => x.GetByUserIdAndSecurityIdAsync(Constants.User.RootUserId, security.Id), Times.Once);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(
            x => x.AddAsync(It.Is<RecurringSchedule>(s => s.UserId == Constants.User.RootUserId)), Times.Once);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WithValidUserId_ShouldReturnActiveSchedules()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var security1 = new Security { Id = Guid.NewGuid(), Ticker = "AAPL", SecurityName = "Apple Inc.", SecurityType = SecurityType.Stock };
        var security2 = new Security { Id = Guid.NewGuid(), Ticker = "MSFT", SecurityName = "Microsoft Corp.", SecurityType = SecurityType.Stock };

        var schedules = new List<RecurringSchedule>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SecurityId = security1.Id,
                Platform = "Binance",
                TargetAmount = 100m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Security = security1
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SecurityId = security2.Id,
                Platform = "Trade Republic",
                TargetAmount = 200m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Security = security2
            }
        };

        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(schedules);

        // Act
        var result = await sut.GetActiveByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Ticker == "AAPL");
        result.Should().Contain(s => s.Ticker == "MSFT");
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.GetActiveByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WithNullUserId_ShouldUseRootUserId()
    {
        // Arrange
        var schedules = new List<RecurringSchedule>();

        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetActiveByUserIdAsync(Constants.User.RootUserId))
            .ReturnsAsync(schedules);

        // Act
        var result = await sut.GetActiveByUserIdAsync(null);

        // Assert
        result.Should().BeEmpty();
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(
            x => x.GetActiveByUserIdAsync(Constants.User.RootUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldSoftDeleteSchedule()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var schedule = new RecurringSchedule
        {
            Id = scheduleId,
            UserId = Guid.NewGuid(),
            SecurityId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetByIdAsync(scheduleId))
            .ReturnsAsync(schedule);
        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.UpdateAsync(It.IsAny<RecurringSchedule>()))
            .ReturnsAsync((RecurringSchedule s) => s);

        // Act
        await sut.DeleteAsync(scheduleId);

        // Assert
        schedule.IsActive.Should().BeFalse();
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.GetByIdAsync(scheduleId), Times.Once);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(
            x => x.UpdateAsync(It.Is<RecurringSchedule>(s => s.Id == scheduleId && !s.IsActive)), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        autoMocker.GetMock<IRecurringScheduleRepository>()
            .Setup(x => x.GetByIdAsync(invalidId))
            .ReturnsAsync((RecurringSchedule?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteAsync(invalidId));
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.GetByIdAsync(invalidId), Times.Once);
        autoMocker.GetMock<IRecurringScheduleRepository>().Verify(x => x.UpdateAsync(It.IsAny<RecurringSchedule>()), Times.Never);
    }
}

