using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ISecurityRepository
{
    Task<Security?> GetByTickerAsync(string ticker);
    Task<Dictionary<string, Security>> GetByTickersAsync(IEnumerable<string> tickers);
    Task<List<Security>> GetByIdsAsync(IEnumerable<Guid> securityIds);
    Task<Security> AddOrUpdateAsync(Security security);
    Task<IEnumerable<Security>> GetAllAsync();
    Task<bool> DeleteAsync(string ticker);
}

