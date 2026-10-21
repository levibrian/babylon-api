using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IInvestorProfileRepository
{
    Task<InvestorProfile?> GetByUserIdAsync(Guid userId);
    Task<InvestorProfile> AddOrUpdateAsync(InvestorProfile profile);
}
