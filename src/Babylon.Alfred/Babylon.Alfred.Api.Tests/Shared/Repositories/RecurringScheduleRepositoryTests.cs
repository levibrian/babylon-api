using AutoFixture;
using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class RecurringScheduleRepositoryTests : IDisposable
{
    private readonly Fixture fixture = new();
    private readonly BabylonDbContext context;
    private readonly RecurringScheduleRepository sut;

    public RecurringScheduleRepositoryTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new RecurringScheduleRepository(context);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    private async Task<User> CreateUserAsync(Guid? userId = null)
    {
        var user = new User
        {
            Id = userId ?? Guid.NewGuid(),
            Username = fixture.Create<string>(),
            Password = fixture.Create<string>(),
            Email = fixture.Create<string>()
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<Security> CreateSecurityAsync(string ticker, string securityName = null!)
    {
        var security = new Security
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            SecurityName = securityName ?? $"{ticker} Inc.",
            SecurityType = SecurityType.Stock
        };
        await context.Securities.AddAsync(security);
        await context.SaveChangesAsync();
        return security;
    }

    [Fact]
    public async Task GetByUserIdAndSecurityIdAsync_WithValidIds_ShouldReturnSchedule()
    {
        // Arrange
        var user = await CreateUserAsync();
        var security = await CreateSecurityAsync("AAPL");
        var schedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.RecurringSchedules.AddAsync(schedule);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByUserIdAndSecurityIdAsync(user.Id, security.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(schedule.Id);
        result.UserId.Should().Be(user.Id);
        result.SecurityId.Should().Be(security.Id);
        result.Platform.Should().Be("Binance");
        result.TargetAmount.Should().Be(100m);
        result.Security.Should().NotBeNull();
        result.Security.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetByUserIdAndSecurityIdAsync_WithInvalidIds_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();

        // Act
        var result = await sut.GetByUserIdAndSecurityIdAsync(userId, securityId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WithActiveSchedules_ShouldReturnOnlyActiveSchedules()
    {
        // Arrange
        var user = await CreateUserAsync();
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("MSFT");
        var security3 = await CreateSecurityAsync("GOOGL");

        var activeSchedule1 = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security1.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var activeSchedule2 = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security2.Id,
            Platform = "Trade Republic",
            TargetAmount = 200m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveSchedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security3.Id,
            Platform = "Binance",
            TargetAmount = 300m,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        await context.RecurringSchedules.AddRangeAsync(activeSchedule1, activeSchedule2, inactiveSchedule);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetActiveByUserIdAsync(user.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.IsActive);
        result.Should().Contain(s => s.SecurityId == security1.Id);
        result.Should().Contain(s => s.SecurityId == security2.Id);
        result.Should().NotContain(s => s.SecurityId == security3.Id);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ShouldOrderByPlatformThenTicker()
    {
        // Arrange
        var user = await CreateUserAsync();
        var security1 = await CreateSecurityAsync("AAPL");
        var security2 = await CreateSecurityAsync("MSFT");
        var security3 = await CreateSecurityAsync("GOOGL");

        var schedule1 = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security1.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var schedule2 = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security2.Id,
            Platform = "Trade Republic",
            TargetAmount = 200m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var schedule3 = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security3.Id,
            Platform = "Binance",
            TargetAmount = 300m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.RecurringSchedules.AddRangeAsync(schedule1, schedule2, schedule3);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetActiveByUserIdAsync(user.Id);

        // Assert
        result.Should().HaveCount(3);
        result[0].Platform.Should().Be("Binance");
        result[0].Security.Ticker.Should().Be("AAPL");
        result[1].Platform.Should().Be("Binance");
        result[1].Security.Ticker.Should().Be("GOOGL");
        result[2].Platform.Should().Be("Trade Republic");
        result[2].Security.Ticker.Should().Be("MSFT");
    }

    [Fact]
    public async Task AddAsync_WithValidSchedule_ShouldAddAndReturnSchedule()
    {
        // Arrange
        var user = await CreateUserAsync();
        var security = await CreateSecurityAsync("AAPL");
        var schedule = new RecurringSchedule
        {
            UserId = user.Id,
            SecurityId = security.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await sut.AddAsync(schedule);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.UserId.Should().Be(user.Id);
        result.SecurityId.Should().Be(security.Id);
        result.Platform.Should().Be("Binance");
        result.TargetAmount.Should().Be(100m);

        var savedSchedule = await context.RecurringSchedules.FindAsync(result.Id);
        savedSchedule.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithValidSchedule_ShouldUpdateAndReturnSchedule()
    {
        // Arrange
        var user = await CreateUserAsync();
        var security = await CreateSecurityAsync("AAPL");
        var schedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.RecurringSchedules.AddAsync(schedule);
        await context.SaveChangesAsync();

        schedule.Platform = "Trade Republic";
        schedule.TargetAmount = 200m;

        // Act
        var result = await sut.UpdateAsync(schedule);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("Trade Republic");
        result.TargetAmount.Should().Be(200m);

        var updatedSchedule = await context.RecurringSchedules.FindAsync(schedule.Id);
        updatedSchedule.Should().NotBeNull();
        updatedSchedule!.Platform.Should().Be("Trade Republic");
        updatedSchedule.TargetAmount.Should().Be(200m);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnSchedule()
    {
        // Arrange
        var user = await CreateUserAsync();
        var security = await CreateSecurityAsync("AAPL");
        var schedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SecurityId = security.Id,
            Platform = "Binance",
            TargetAmount = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.RecurringSchedules.AddAsync(schedule);
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetByIdAsync(schedule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(schedule.Id);
        result.Security.Should().NotBeNull();
        result.Security.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await sut.GetByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }
}

