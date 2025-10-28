using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ICompanyService
{
    Task<IList<CompanyDto>> GetAllAsync();
    Task<CompanyDto?> GetByTickerAsync(string ticker);
    Task<Company> CreateAsync(CreateCompanyRequest request);
    Task<Company?> UpdateAsync(string ticker, UpdateCompanyRequest request);
    Task<bool> DeleteAsync(string ticker);
}
