using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

public class AccountLinkingService(
    IUserRepository userRepository,
    ILogger<AccountLinkingService> logger) : IAccountLinkingService
{
    public async Task<User> GetOrCreateGoogleUserAsync(string email, string googleSubject)
    {
        var existingUser = await userRepository.GetUserByEmailAsync(email);

        if (existingUser != null)
        {
            logger.LogInformation(
                "Linking Google auth to existing account: {Email}, UserId: {UserId}",
                email,
                existingUser.Id);

            await LinkGoogleToAccountAsync(existingUser);
            return existingUser;
        }

        logger.LogInformation("Creating new user from Google login: {Email}", email);

        var newUser = new User
        {
            Email = email,
            Username = email, // Default to email, user can change later
            AuthProvider = "Google",
            CreatedAt = DateTime.UtcNow,
            MonthlyInvestmentAmount = 0
        };

        return await userRepository.CreateUserAsync(newUser);
    }

    public async Task LinkGoogleToAccountAsync(User user)
    {
        if (user.HasGoogleAuth)
        {
            return; // Already linked
        }

        UpdateAuthProvider(user);
        await userRepository.UpdateUserAsync(user);

        logger.LogInformation(
            "Google auth linked to account: {Email}, AuthProvider: {AuthProvider}",
            user.Email,
            user.AuthProvider);
    }

    public async Task LinkLocalToAccountAsync(User user, string passwordHash)
    {
        if (user.HasLocalAuth)
        {
            throw new InvalidOperationException("Account already has password authentication");
        }

        user.Password = passwordHash;
        UpdateAuthProvider(user);
        await userRepository.UpdateUserAsync(user);

        logger.LogInformation(
            "Local auth linked to account: {Email}, AuthProvider: {AuthProvider}",
            user.Email,
            user.AuthProvider);
    }

    public void UpdateAuthProvider(User user)
    {
        var hasLocal = user.HasLocalAuth;
        var hasGoogle = user.HasGoogleAuth;

        user.AuthProvider = (hasLocal, hasGoogle) switch
        {
            (true, true) => "Local,Google",
            (true, false) => "Local",
            (false, true) => "Google",
            (false, false) => null
        };
    }
}
