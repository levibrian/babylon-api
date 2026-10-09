using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

public interface IAuthService
{
    Task<AuthResponse> GoogleLoginAsync(string idToken);
    Task<User> AuthenticateGoogleUserAsync(string idToken);
    Task<AuthResponse> LoginAsync(string emailOrUsername, string password);
    Task<AuthResponse> RegisterAsync(string username, string email, string password);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
