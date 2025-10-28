using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class CompanyService(ICompanyRepository companyRepository) : ICompanyService
{
    public async Task<IList<CompanyDto>> GetAllAsync()
    {
        var companies = await companyRepository.GetAllAsync();

        return companies
            .Select(x => new CompanyDto(x.Ticker, x.CompanyName))
            .ToList();
    }

    public async Task<CompanyDto?> GetByTickerAsync(string ticker)
    {
        var company = await companyRepository.GetByTickerAsync(ticker);

        if (company is null)
        {
            throw new InvalidOperationException("Company provided not found in our internal database.");
        }

        return new CompanyDto(company.Ticker, company.CompanyName);
    }

    public async Task<Company> CreateAsync(CreateCompanyRequest request)
    {
        var company = new Company
        {
            Ticker = request.Ticker,
            CompanyName = request.CompanyName,
            LastUpdated = DateTime.UtcNow
        };

        return await companyRepository.AddOrUpdateAsync(company);
    }

    public async Task<Company?> UpdateAsync(string ticker, UpdateCompanyRequest request)
    {
        var existingCompany = await companyRepository.GetByTickerAsync(ticker);
        if (existingCompany == null)
        {
            return null;
        }

        existingCompany.CompanyName = request.CompanyName;
        existingCompany.LastUpdated = DateTime.UtcNow;

        return await companyRepository.AddOrUpdateAsync(existingCompany);
    }

    public async Task<bool> DeleteAsync(string ticker)
    {
        return await companyRepository.DeleteAsync(ticker);
    }
}
