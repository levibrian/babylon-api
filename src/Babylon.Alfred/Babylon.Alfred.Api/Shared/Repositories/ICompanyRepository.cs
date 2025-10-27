using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByTickerAsync(string ticker);
    Task<Company> AddOrUpdateAsync(Company company);
    Task<IEnumerable<Company>> GetAllAsync();
}

