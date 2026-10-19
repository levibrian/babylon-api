using Babylon.Alfred.Mcp.Logging;
using FluentAssertions;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Mcp.Logging;

public class EndpointNameVersionEnricherTests
{
    [Theory]
    [InlineData("Server (Babylon.Alfred.Mcp 1.0.0.0)", "Server")]
    [InlineData("Client (Babylon.Alfred.Mcp 1.2.3)", "Client")]
    [InlineData("Server (Babylon.Alfred.Mcp)", "Server")]
    public void StripName_WithParenthesizedSuffix_ShouldRemoveSuffix(string input, string expected)
    {
        // Act
        var result = EndpointNameVersionEnricher.StripName(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void StripName_WithoutParens_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Server";

        // Act
        var result = EndpointNameVersionEnricher.StripName(input);

        // Assert
        result.Should().Be(input);
    }
}
