using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserAsync(Guid userId);
    Task UpdateUserAsync(User user);
}
