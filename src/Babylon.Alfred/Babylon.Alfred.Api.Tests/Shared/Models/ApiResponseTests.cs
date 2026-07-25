using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Shared.Models;

public class ApiResponseTests
{
    [Fact]
    public void Ok_SetsSuccessTrueAndData()
    {
        var response = ApiResponse<string>.Ok("hello");

        response.Success.Should().BeTrue();
        response.Data.Should().Be("hello");
        response.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_SetsSuccessFalseAndError()
    {
        var response = ApiResponse<string>.Fail("something went wrong");

        response.Success.Should().BeFalse();
        response.Error.Should().Be("something went wrong");
        response.Data.Should().BeNull();
    }

    [Fact]
    public void Ok_WithObject_SetsDataCorrectly()
    {
        var data = new { Id = 1, Name = "Test" };
        var response = ApiResponse<object>.Ok(data);

        response.Success.Should().BeTrue();
        response.Data.Should().Be(data);
    }
}
