using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ICashBalanceRepository
{
    Task<CashBalance?> GetByUserIdAsync(Guid userId);
    Task<CashBalance> AddOrUpdateAsync(CashBalance cashBalance);
}
