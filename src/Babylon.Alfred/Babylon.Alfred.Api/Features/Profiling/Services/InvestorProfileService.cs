using Babylon.Alfred.Api.Features.Profiling.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Profiling.Services;

public class InvestorProfileService(IInvestorProfileRepository profileRepository) : IInvestorProfileService
{
    public async Task<InvestorProfileDto> GetProfile(Guid userId)
    {
        var profile = await profileRepository.GetByUserIdAsync(userId);
        return ToDto(profile);
    }

    public async Task<InvestorProfileDto> SaveProfile(Guid userId, string profileMarkdown, string rawAnswers, string rubricVersion)
    {
        if (string.IsNullOrWhiteSpace(profileMarkdown))
        {
            throw new ArgumentException("profileMarkdown is required", nameof(profileMarkdown));
        }

        if (string.IsNullOrWhiteSpace(rawAnswers))
        {
            throw new ArgumentException("rawAnswers is required", nameof(rawAnswers));
        }

        if (string.IsNullOrWhiteSpace(rubricVersion))
        {
            throw new ArgumentException("rubricVersion is required", nameof(rubricVersion));
        }

        var profile = new InvestorProfile
        {
            UserId = userId,
            ProfileMarkdown = profileMarkdown,
            RawAnswers = rawAnswers,
            RubricVersion = rubricVersion,
            UpdatedAt = DateTime.UtcNow
        };

        var saved = await profileRepository.AddOrUpdateAsync(profile);
        return ToDto(saved);
    }

    private static InvestorProfileDto ToDto(InvestorProfile? profile)
    {
        if (profile == null)
        {
            return new InvestorProfileDto();
        }

        return new InvestorProfileDto
        {
            ProfileMarkdown = profile.ProfileMarkdown,
            RawAnswers = profile.RawAnswers,
            RubricVersion = profile.RubricVersion,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
