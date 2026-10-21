using Babylon.Alfred.Api.Features.Profiling.Models.Responses;

namespace Babylon.Alfred.Api.Features.Profiling.Services;

public interface IInvestorProfileService
{
    Task<InvestorProfileDto> GetProfile(Guid userId);
    Task<InvestorProfileDto> SaveProfile(Guid userId, string profileMarkdown, string rawAnswers, string rubricVersion);
}
