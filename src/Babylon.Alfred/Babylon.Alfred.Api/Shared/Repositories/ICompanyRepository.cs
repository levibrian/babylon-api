using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByTickerAsync(string ticker);
    Task<Dictionary<string, Company>> GetByTickersAsync(IEnumerable<string> tickers);
    Task<Company> AddOrUpdateAsync(Company company);
    Task<IEnumerable<Company>> GetAllAsync();
    Task<bool> DeleteAsync(string ticker);
}

