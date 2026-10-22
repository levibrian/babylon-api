using System.Net.Http.Headers;
using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Shared.Models;

namespace Babylon.Alfred.Api.IntegrationTests;

public static class TestAuthHelper
{
    /// <summary>Registers a fresh random user and attaches the resulting token to the client's default headers.</summary>
    public static async Task<AuthResponse> RegisterAndAuthenticateAsync(this HttpClient client)
    {
        var request = new RegisterRequest
        {
            Username = $"user_{Guid.NewGuid():N}"[..20],
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "correct-horse-battery-staple"
        };

        var response = await client.PostJsonAsync("/api/v1/auth/register", request);
        var body = await response.ReadJsonAsync<ApiResponse<AuthResponse>>();

        if (body?.Data is null)
        {
            throw new InvalidOperationException($"Register failed: {await response.Content.ReadAsStringAsync()}");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Data.Token);
        return body.Data;
    }
}
