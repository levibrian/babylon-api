using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class SecurityService(ISecurityRepository securityRepository) : ISecurityService
{
    public async Task<IList<CompanyDto>> GetAllAsync()
    {
        var securities = await securityRepository.GetAllAsync();

        return securities
            .Select(x => new CompanyDto(x.Ticker, x.CompanyName))
            .ToList();
    }

    public async Task<CompanyDto?> GetByTickerAsync(string ticker)
    {
        var security = await securityRepository.GetByTickerAsync(ticker);

        if (security is null)
        {
            throw new InvalidOperationException("Security provided not found in our internal database.");
        }

        return new CompanyDto(security.Ticker, security.CompanyName);
    }

    public async Task<Security> CreateAsync(CreateCompanyRequest request)
    {
        var security = new Security
        {
            Ticker = request.Ticker,
            CompanyName = request.CompanyName,
            LastUpdated = DateTime.UtcNow
        };

        return await securityRepository.AddOrUpdateAsync(security);
    }

    public async Task<Security?> UpdateAsync(string ticker, UpdateCompanyRequest request)
    {
        var existingSecurity = await securityRepository.GetByTickerAsync(ticker);
        if (existingSecurity == null)
        {
            return null;
        }

        existingSecurity.CompanyName = request.CompanyName;
        existingSecurity.LastUpdated = DateTime.UtcNow;

        return await securityRepository.AddOrUpdateAsync(existingSecurity);
    }

    public async Task<bool> DeleteAsync(string ticker)
    {
        return await securityRepository.DeleteAsync(ticker);
    }
}

