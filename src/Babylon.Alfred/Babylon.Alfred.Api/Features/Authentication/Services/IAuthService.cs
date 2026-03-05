using Babylon.Alfred.Api.Features.Authentication.Models;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

public interface IAuthService
{
    Task<AuthResponse> GoogleLoginAsync(string idToken);
    Task<AuthResponse> LoginAsync(string emailOrUsername, string password);
    Task<AuthResponse> RegisterAsync(string username, string email, string password);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
