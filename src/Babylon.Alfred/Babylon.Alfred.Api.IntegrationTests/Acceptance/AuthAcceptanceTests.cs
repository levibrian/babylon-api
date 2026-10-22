using System.Net;
using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.IntegrationTests.Acceptance;

[Collection(IntegrationTestCollection.Name)]
public class AuthAcceptanceTests(BabylonApiFactory factory)
{
    [Fact]
    public async Task Register_ThenLogin_ReturnsWorkingAccessToken()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Username = $"user_{Guid.NewGuid():N}"[..20],
            Email = email,
            Password = "correct-horse-battery-staple"
        };

        // Act — register
        var registerResponse = await client.PostJsonAsync("/api/v1/auth/register", registerRequest);
        var registerBody = await registerResponse.ReadJsonAsync<ApiResponse<AuthResponse>>();

        // Assert — register succeeded and returned a usable token
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        registerBody!.Success.Should().BeTrue();
        registerBody.Data!.Token.Should().NotBeNullOrWhiteSpace();
        registerBody.Data.Email.Should().Be(email);

        // Act — login with the same credentials
        var loginResponse = await client.PostJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            EmailOrUsername = email,
            Password = registerRequest.Password
        });
        var loginBody = await loginResponse.ReadJsonAsync<ApiResponse<AuthResponse>>();

        // Assert — login succeeded
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginBody!.Data!.UserId.Should().Be(registerBody.Data.UserId);

        // Act — the register token authenticates a protected endpoint
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerBody.Data.Token);
        var portfolioResponse = await client.GetAsync("/api/v1/portfolios");

        // Assert — protected endpoint accepts the token
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/portfolios");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
