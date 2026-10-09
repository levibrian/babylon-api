using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Controllers;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Controllers;

public class AllocationsControllerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly AllocationsController sut;
    private readonly Guid userId = Guid.NewGuid();

    public AllocationsControllerTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<AllocationsController>();
        var identity = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) }, "TestAuthType");
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Get_ShouldReturnOkWithAuthenticatedUsersTargets()
    {
        // Arrange
        var targets = fixture.CreateMany<AllocationTargetDto>().ToList();
        autoMocker.GetMock<IAllocationService>()
            .Setup(x => x.GetTargets(userId))
            .ReturnsAsync(targets);

        // Act
        var result = await sut.Get();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IList<AllocationTargetDto>>>().Subject;
        apiResponse.Data.Should().BeEquivalentTo(targets);
    }

    [Fact]
    public async Task Put_ShouldReplaceAuthenticatedUsersTargetsAndReturnOk()
    {
        // Arrange
        var request = fixture.Create<UpdateAllocationTargetsRequest>();
        var targets = fixture.CreateMany<AllocationTargetDto>().ToList();
        autoMocker.GetMock<IAllocationService>()
            .Setup(x => x.ReplaceTargets(userId, request))
            .ReturnsAsync(targets);

        // Act
        var result = await sut.Put(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IList<AllocationTargetDto>>>().Subject;
        apiResponse.Data.Should().BeEquivalentTo(targets);
        autoMocker.GetMock<IAllocationService>().Verify(x => x.ReplaceTargets(userId, request), Times.Once);
    }
}
