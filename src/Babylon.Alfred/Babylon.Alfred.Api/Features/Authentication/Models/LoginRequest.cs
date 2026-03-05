namespace Babylon.Alfred.Api.Features.Authentication.Models;

public class LoginRequest
{
    public string EmailOrUsername { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
