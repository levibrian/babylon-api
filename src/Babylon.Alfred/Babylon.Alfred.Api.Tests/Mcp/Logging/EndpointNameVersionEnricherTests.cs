using Babylon.Alfred.Mcp.Logging;
using FluentAssertions;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Mcp.Logging;

public class EndpointNameVersionEnricherTests
{
    [Theory]
    [InlineData("Server (Babylon.Alfred.Mcp 1.0.0.0)", "Server (Babylon.Alfred.Mcp)")]
    [InlineData("Client (Babylon.Alfred.Mcp 1.2.3)", "Client (Babylon.Alfred.Mcp)")]
    public void StripVersion_WithVersionedEndpointName_ShouldRemoveVersion(string input, string expected)
    {
        // Act
        var result = EndpointNameVersionEnricher.StripVersion(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void StripVersion_WithoutVersion_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Server (Babylon.Alfred.Mcp)";

        // Act
        var result = EndpointNameVersionEnricher.StripVersion(input);

        // Assert
        result.Should().Be(input);
    }
}
