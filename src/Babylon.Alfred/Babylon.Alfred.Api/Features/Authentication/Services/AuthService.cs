using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Utils;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Google.Apis.Auth;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

public class AuthService(
    IUserRepository userRepository,
    JwtTokenGenerator jwtTokenGenerator,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> GoogleLoginAsync(string idToken)
    {
        try
        {
            var googleClientId = configuration["Authentication:Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var user = await userRepository.GetUserByEmailAsync(payload.Email);

            if (user == null)
            {
                logger.LogInformation("Creating new user from Google login: {Email}", payload.Email);

                // Create new user
                user = new User
                {
                    Email = payload.Email,
                    Username = payload.Email, // Default username to email
                    AuthProvider = "Google",
                    CreatedAt = DateTime.UtcNow,
                    MonthlyInvestmentAmount = 0 // Default
                };

                await userRepository.CreateUserAsync(user);
            }
            else if (user.AuthProvider == "Local" && string.IsNullOrEmpty(user.Password))
            {
                 // Edge case: Determine if we should link accounts or just log them in.
                 // For now, if they exist, we just log them in, maybe updating AuthProvider if null
                 if (string.IsNullOrEmpty(user.AuthProvider))
                 {
                     user.AuthProvider = "Google";
                     await userRepository.UpdateUserAsync(user);
                 }
            }

            var token = jwtTokenGenerator.GenerateToken(user);

            return new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                AuthProvider = user.AuthProvider
            };
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Invalid Google ID Token");
            throw new UnauthorizedAccessException("Invalid Google token", ex);
        }
    }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        var user = await userRepository.GetUserByUsernameAsync(username);

        if (user == null || user.AuthProvider == "Google" || string.IsNullOrEmpty(user.Password))
        {
            // Don't verify password for Google users or if user not found (security best practice: timing attacks avoidance generic message usually, but here simple logic)
            // If AuthProvider is Google, they shouldn't use password login unless we support multiple auth methods per user (hybrid).
            // For this implementation, we assume separation.
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AuthProvider = user.AuthProvider
        };
    }

    public async Task<AuthResponse> RegisterAsync(string username, string email, string password)
    {
        var existingUserEmail = await userRepository.GetUserByEmailAsync(email);
        if (existingUserEmail != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var existingUserUsername = await userRepository.GetUserByUsernameAsync(username);
        if (existingUserUsername != null)
        {
            throw new InvalidOperationException("Username is already taken");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Username = username,
            Email = email,
            Password = passwordHash,
            AuthProvider = "Local",
            CreatedAt = DateTime.UtcNow,
            MonthlyInvestmentAmount = 0
        };

        await userRepository.CreateUserAsync(user);

        var token = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AuthProvider = user.AuthProvider
        };
    }
}
