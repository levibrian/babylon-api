using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Authentication.Controllers;

[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : BabylonControllerBase
{
    [HttpPost("google")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await authService.GoogleLoginAsync(request.IdToken);
        return Success(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request.EmailOrUsername, request.Password);
        return Success(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request.Username, request.Email, request.Password);
        return Success(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);
        return Success(result);
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenRequest request)
    {
        await authService.LogoutAsync(request.RefreshToken);
        return Success();
    }
}
