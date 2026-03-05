using Babylon.Alfred.Api.Features.Authentication.Models;
using Babylon.Alfred.Api.Features.Authentication.Utils;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Google.Apis.Auth;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAccountLinkingService accountLinkingService,
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

            // Get or create user, automatically linking to existing account by email
            var user = await accountLinkingService.GetOrCreateGoogleUserAsync(
                payload.Email,
                payload.Subject);

            return await GenerateAuthResponseAsync(user);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Invalid Google ID Token");
            throw new UnauthorizedAccessException("Invalid Google token", ex);
        }
    }

    public async Task<AuthResponse> LoginAsync(string emailOrUsername, string password)
    {
        var user = await userRepository.GetUserByEmailOrUsernameAsync(emailOrUsername);

        if (user == null || !user.HasLocalAuth)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password!))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(string username, string email, string password)
    {
        var existingUserByEmail = await userRepository.GetUserByEmailAsync(email);

        // If user exists with Google-only auth, link local auth
        if (existingUserByEmail != null)
        {
            if (existingUserByEmail.HasLocalAuth)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Link local auth to existing Google account
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            await accountLinkingService.LinkLocalToAccountAsync(existingUserByEmail, passwordHash);

            logger.LogInformation(
                "Linked local auth to existing Google account: {Email}",
                email);

            return await GenerateAuthResponseAsync(existingUserByEmail);
        }

        var existingUserByUsername = await userRepository.GetUserByUsernameAsync(username);
        if (existingUserByUsername != null)
        {
            throw new InvalidOperationException("Username is already taken");
        }

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Username = username,
            Email = email,
            Password = newPasswordHash,
            AuthProvider = "Local",
            CreatedAt = DateTime.UtcNow,
            MonthlyInvestmentAmount = 0
        };

        await userRepository.CreateUserAsync(user);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Revoke current token (single-use)
        storedToken.IsRevoked = true;
        await refreshTokenRepository.UpdateAsync(storedToken);

        return await GenerateAuthResponseAsync(storedToken.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            await refreshTokenRepository.UpdateAsync(storedToken);
        }
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var token = jwtTokenGenerator.GenerateToken(user);
        await refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AuthProvider = user.AuthProvider
        };
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(Guid userId)
    {
        var refreshTokenStr = jwtTokenGenerator.GenerateRefreshToken();
        var expirationDays = int.Parse(configuration["Authentication:Jwt:RefreshTokenExpirationDays"] ?? "7");

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenStr,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await refreshTokenRepository.AddAsync(refreshToken);
        return refreshTokenStr;
    }
}
