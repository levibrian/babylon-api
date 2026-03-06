using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

public class PasswordService(
    IUserRepository userRepository,
    IAccountLinkingService accountLinkingService,
    ILogger<PasswordService> logger) : IPasswordService
{
    public async Task UpdatePassword(Guid userId, string? currentPassword, string newPassword)
    {
        var user = await userRepository.GetUserAsync(userId);

        if (user is null)
        {
            throw new InvalidOperationException($"User {userId} not found.");
        }

        if (user.HasLocalAuth)
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                throw new UnauthorizedAccessException("Current password is required");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password!))
            {
                throw new UnauthorizedAccessException("Invalid current password");
            }
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        accountLinkingService.UpdateAuthProvider(user);

        await userRepository.UpdateUserAsync(user);

        logger.LogPasswordUpdated(userId);
    }
}
