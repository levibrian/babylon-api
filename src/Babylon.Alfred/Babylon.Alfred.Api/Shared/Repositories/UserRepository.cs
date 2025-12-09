using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class UserRepository(BabylonDbContext context) : IUserRepository
{
    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await context.Users.FindAsync(userId);
    }

    public async Task UpdateUserAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }
}
