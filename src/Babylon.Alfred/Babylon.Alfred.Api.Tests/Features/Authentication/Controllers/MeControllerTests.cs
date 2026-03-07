using System.Security.Claims;
using Babylon.Alfred.Api.Features.Authentication.Controllers;
using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Authentication.Controllers;

public class MeControllerTests
{
    private readonly AutoMocker autoMocker = new();

    private MeController CreateSutWithUserId(Guid userId)
    {
        var controller = autoMocker.CreateInstance<MeController>();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                ]))
            }
        };
        return controller;
    }

    [Fact]
    public async Task UpdatePassword_WithValidRequest_ShouldReturnOkWithEmptyData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sut = CreateSutWithUserId(userId);
        var request = new UpdatePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            Password = "NewPass1!"
        };

        // Act
        var result = await sut.UpdatePassword(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePassword_WithValidRequest_ShouldCallServiceWithUserIdFromToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sut = CreateSutWithUserId(userId);
        var request = new UpdatePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            Password = "NewPass1!"
        };

        // Act
        await sut.UpdatePassword(request);

        // Assert
        autoMocker.GetMock<IPasswordService>()
            .Verify(x => x.UpdatePassword(userId, request.CurrentPassword, request.Password), Times.Once);
    }
}
