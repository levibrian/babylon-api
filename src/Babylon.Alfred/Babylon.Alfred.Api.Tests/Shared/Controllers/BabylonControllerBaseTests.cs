using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Tests.Shared.Controllers;

public class BabylonControllerBaseTests
{
    private readonly TestController _sut;

    public BabylonControllerBaseTests()
    {
        _sut = new TestController();
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void Success_WithData_Returns200WithWrappedData()
    {
        var result = _sut.CallSuccess("hello") as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var body = result.Value as ApiResponse<string>;
        body!.Success.Should().BeTrue();
        body.Data.Should().Be("hello");
        body.Error.Should().BeNull();
    }

    [Fact]
    public void Success_Void_Returns200WithEmptyData()
    {
        var result = _sut.CallSuccessVoid() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var body = result.Value as ApiResponse<object>;
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public void Fail_Returns400ByDefault()
    {
        var result = _sut.CallFail("bad input") as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
        var body = result.Value as ApiResponse<object>;
        body!.Success.Should().BeFalse();
        body.Error.Should().Be("bad input");
    }

    [Fact]
    public void Fail_WithCustomStatus_ReturnsCorrectStatus()
    {
        var result = _sut.CallFail("not found", 404) as ObjectResult;

        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Created_Returns201WithWrappedData()
    {
        var result = _sut.CallCreated("new-resource") as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        var body = result.Value as ApiResponse<string>;
        body!.Success.Should().BeTrue();
        body.Data.Should().Be("new-resource");
    }

    private class TestController : BabylonControllerBase
    {
        public IActionResult CallSuccess<T>(T data) => Success(data);
        public IActionResult CallSuccessVoid() => Success();
        public IActionResult CallFail(string error, int status = 400) => Fail(error, status);
        public IActionResult CallCreated<T>(T data) => Created(data);
    }
}
