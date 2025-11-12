using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly BabylonDbContext context;

    public CompanyRepository(BabylonDbContext context)
    {
        this.context = context;
    }

    public async Task<Company?> GetByTickerAsync(string ticker)
    {
        return await context.Companies.FirstOrDefaultAsync(c => c.Ticker == ticker);
    }

    public async Task<Dictionary<string, Company>> GetByTickersAsync(IEnumerable<string> tickers)
    {
        var tickerList = tickers.ToList();
        if (tickerList.Count == 0)
        {
            return new Dictionary<string, Company>();
        }

        var companies = await context.Companies
            .Where(c => tickerList.Contains(c.Ticker))
            .ToListAsync();

        return companies.ToDictionary(c => c.Ticker, c => c);
    }

    public async Task<List<Company>> GetByIdsAsync(IEnumerable<Guid> companyIds)
    {
        var companyIdList = companyIds.ToList();
        if (companyIdList.Count == 0)
        {
            return new List<Company>();
        }

        return await context.Companies
            .Where(c => companyIdList.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<Company> AddOrUpdateAsync(Company company)
    {
        // Find by Ticker (since Ticker has unique index)
        var existing = await context.Companies.FirstOrDefaultAsync(c => c.Ticker == company.Ticker);

        if (existing != null)
        {
            existing.CompanyName = company.CompanyName;
            existing.LastUpdated = DateTime.UtcNow;
            context.Companies.Update(existing);
        }
        else
        {
            // Generate Id if not set
            if (company.Id == Guid.Empty)
            {
                company.Id = Guid.NewGuid();
            }
            company.LastUpdated = DateTime.UtcNow;
            await context.Companies.AddAsync(company);
        }

        await context.SaveChangesAsync();
        return existing ?? company;
    }

    public async Task<IEnumerable<Company>> GetAllAsync()
    {
        return await context.Companies.ToListAsync();
    }

    public async Task<bool> DeleteAsync(string ticker)
    {
        var company = await GetByTickerAsync(ticker);
        if (company == null)
        {
            return false;
        }

        context.Companies.Remove(company);
        await context.SaveChangesAsync();
        return true;
    }
}

