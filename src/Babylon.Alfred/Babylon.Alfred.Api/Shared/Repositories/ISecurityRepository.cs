using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ISecurityRepository
{
    Task<Security?> GetByTickerAsync(string ticker);
    Task<Dictionary<string, Security>> GetByTickersAsync(IEnumerable<string> tickers);
    Task<List<Security>> GetByIdsAsync(IEnumerable<Guid> securityIds);

    /// <summary>
    /// Gets the first security matching the specified ISIN.
    /// Note: ISIN is not unique as multiple tickers can share the same ISIN (e.g., different exchanges or share classes).
    /// Returns the first match ordered by ticker alphabetically.
    /// </summary>
    Task<Security?> GetByIsinAsync(string isin);

    /// <summary>
    /// Gets all securities matching the specified ISIN.
    /// Use this when you need to handle multiple tickers for the same underlying instrument.
    /// </summary>
    Task<List<Security>> GetAllByIsinAsync(string isin);

    Task<Security> AddOrUpdateAsync(Security security);
    Task<IEnumerable<Security>> GetAllAsync();
    Task<bool> DeleteAsync(string ticker);
}

