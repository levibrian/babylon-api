namespace Babylon.Alfred.Api.Features.Authentication.Services;

public interface IPasswordService
{
    Task UpdatePassword(Guid userId, string? currentPassword, string newPassword);
}
