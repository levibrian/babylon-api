using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class InvestorProfileRepository(BabylonDbContext context) : IInvestorProfileRepository
{
    public async Task<InvestorProfile?> GetByUserIdAsync(Guid userId)
    {
        return await context.InvestorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<InvestorProfile> AddOrUpdateAsync(InvestorProfile profile)
    {
        var existing = await context.InvestorProfiles
            .FirstOrDefaultAsync(p => p.UserId == profile.UserId);

        if (existing == null)
        {
            await context.InvestorProfiles.AddAsync(profile);
        }
        else
        {
            existing.ProfileMarkdown = profile.ProfileMarkdown;
            existing.RawAnswers = profile.RawAnswers;
            existing.RubricVersion = profile.RubricVersion;
            existing.UpdatedAt = profile.UpdatedAt;
            context.InvestorProfiles.Update(existing);
        }

        await context.SaveChangesAsync();
        return profile;
    }
}
