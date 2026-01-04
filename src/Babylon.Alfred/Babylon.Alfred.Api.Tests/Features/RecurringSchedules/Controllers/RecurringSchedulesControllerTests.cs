using AutoFixture;
using Babylon.Alfred.Api.Features.RecurringSchedules.Controllers;
using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;
using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Responses;
using Babylon.Alfred.Api.Features.RecurringSchedules.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace Babylon.Alfred.Api.Tests.Features.RecurringSchedules.Controllers;

public class RecurringSchedulesControllerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly RecurringSchedulesController sut;

    public RecurringSchedulesControllerTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<RecurringSchedulesController>();

        // Setup Mock User
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
        
        // Store common userId for mocks
        fixture.Inject(userId);
    }

    [Fact]
    public async Task CreateOrUpdateRecurringSchedule_WithValidRequest_ShouldReturnOkWithScheduleDto()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var request = fixture.Build<CreateRecurringScheduleRequest>()
            .With(r => r.Ticker, "AAPL")
            .With(r => r.SecurityName, "Apple Inc.")
            .With(r => r.Platform, "Binance")
            .With(r => r.TargetAmount, 100m)
            .Create();

        var scheduleDto = new RecurringScheduleDto
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker,
            SecurityName = request.SecurityName,
            Platform = request.Platform,
            TargetAmount = request.TargetAmount
        };

        autoMocker.GetMock<IRecurringScheduleService>()
            .Setup(x => x.CreateOrUpdateAsync(userId, request))
            .ReturnsAsync(scheduleDto);

        // Act
        var result = await sut.CreateOrUpdateRecurringSchedule(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(scheduleDto);
        autoMocker.GetMock<IRecurringScheduleService>().Verify(x => x.CreateOrUpdateAsync(userId, request), Times.Once);
    }


    [Fact]
    public async Task GetRecurringSchedules_ShouldReturnOkWithScheduleList()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var schedules = new List<RecurringScheduleDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Ticker = "AAPL",
                SecurityName = "Apple Inc.",
                Platform = "Binance",
                TargetAmount = 100m
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ticker = "MSFT",
                SecurityName = "Microsoft Corp.",
                Platform = "Trade Republic",
                TargetAmount = 200m
            }
        };

        autoMocker.GetMock<IRecurringScheduleService>()
            .Setup(x => x.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(schedules);

        // Act
        var result = await sut.GetRecurringSchedules();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(schedules);
        autoMocker.GetMock<IRecurringScheduleService>().Verify(x => x.GetActiveByUserIdAsync(userId), Times.Once);
    }


    [Fact]
    public async Task DeleteRecurringSchedule_WithValidId_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();

        autoMocker.GetMock<IRecurringScheduleService>()
            .Setup(x => x.DeleteAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await sut.DeleteRecurringSchedule(scheduleId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { message = "Recurring schedule deleted successfully" });
        autoMocker.GetMock<IRecurringScheduleService>().Verify(x => x.DeleteAsync(scheduleId), Times.Once);
    }

    [Fact]
    public async Task DeleteRecurringSchedule_WithInvalidId_ShouldPropagateException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        autoMocker.GetMock<IRecurringScheduleService>()
            .Setup(x => x.DeleteAsync(invalidId))
            .ThrowsAsync(new InvalidOperationException($"Recurring schedule with id {invalidId} not found."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteRecurringSchedule(invalidId));
        autoMocker.GetMock<IRecurringScheduleService>().Verify(x => x.DeleteAsync(invalidId), Times.Once);
    }
}

