using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Mappers;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class SecurityService(
    ISecurityRepository securityRepository,
    IYahooMarketDataService yahooMarketDataService) : ISecurityService
{
    public async Task<IList<CompanyDto>> GetAllAsync()
    {
        var securities = await securityRepository.GetAllAsync();

        return securities
            .Select(x => new CompanyDto(x.Ticker, x.SecurityName, x.SecurityType, x.Currency, x.Exchange, x.Sector, x.Industry, x.Geography, x.MarketCap))
            .ToList();
    }

    public async Task<CompanyDto?> GetByTickerAsync(string ticker)
    {
        var security = await securityRepository.GetByTickerAsync(ticker);

        if (security is null)
        {
            throw new InvalidOperationException("Security provided not found in our internal database.");
        }

        return new CompanyDto(security.Ticker, security.SecurityName, security.SecurityType, security.Currency, security.Exchange, security.Sector, security.Industry, security.Geography, security.MarketCap);
    }

    /// <summary>
    /// Creates or retrieves a security by ticker symbol.
    /// If the security doesn't exist in the database, it fetches metadata from Yahoo Finance.
    /// </summary>
    public async Task<CompanyDto> CreateOrGetByTickerAsync(string ticker)
    {
        // Check if security already exists
        var existingSecurity = await securityRepository.GetByTickerAsync(ticker);
        if (existingSecurity != null)
        {
            return new CompanyDto(
                existingSecurity.Ticker,
                existingSecurity.SecurityName,
                existingSecurity.SecurityType,
                existingSecurity.Currency,
                existingSecurity.Exchange);
        }

        // Fetch from Yahoo Finance
        var results = await yahooMarketDataService.SearchAsync(ticker);
        var result = results.FirstOrDefault(r => r.Symbol.Equals(ticker, StringComparison.OrdinalIgnoreCase));

        if (result == null)
        {
            throw new InvalidOperationException($"Ticker '{ticker}' not found on Yahoo Finance.");
        }

        // Map Yahoo data to Security entity
        var security = new Security
        {
            Ticker = ticker.ToUpperInvariant(),
            SecurityName = !string.IsNullOrWhiteSpace(result.LongName) ? result.LongName : ticker,
            SecurityType = QuoteTypeMapper.ToSecurityType(result.QuoteType),
            Currency = result.Currency,
            Exchange = result.Exchange,
            Sector = result.Sector,
            Industry = result.Industry,
            MarketCap = null, // Search API typically doesn't provide MarketCap
            Geography = GeographyMapper.ToGeography(result.Exchange, result.Currency),
            LastUpdated = DateTime.UtcNow
        };

        var savedSecurity = await securityRepository.AddOrUpdateAsync(security);
        return new CompanyDto(
            savedSecurity.Ticker,
            savedSecurity.SecurityName,
            savedSecurity.SecurityType,
            savedSecurity.Currency,
            savedSecurity.Exchange,
            savedSecurity.Sector,
            savedSecurity.Industry,
            savedSecurity.Geography,
            savedSecurity.MarketCap);
    }

    public async Task<Security> CreateAsync(CreateCompanyRequest request)
    {
        var security = new Security
        {
            Ticker = request.Ticker,
            SecurityName = request.SecurityName,
            SecurityType = request.SecurityType,
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

        existingSecurity.SecurityName = request.SecurityName;
        if (request.SecurityType.HasValue)
        {
            existingSecurity.SecurityType = request.SecurityType.Value;
        }
        existingSecurity.LastUpdated = DateTime.UtcNow;

        return await securityRepository.AddOrUpdateAsync(existingSecurity);
    }

    public async Task<bool> DeleteAsync(string ticker)
    {
        return await securityRepository.DeleteAsync(ticker);
    }
}

