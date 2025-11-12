using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class SecurityRepository(BabylonDbContext context) : ISecurityRepository
{
    public async Task<Security?> GetByTickerAsync(string ticker)
    {
        return await context.Securities.FirstOrDefaultAsync(c => c.Ticker == ticker);
    }

    public async Task<Dictionary<string, Security>> GetByTickersAsync(IEnumerable<string> tickers)
    {
        var tickerList = tickers.ToList();
        if (tickerList.Count == 0)
        {
            return new Dictionary<string, Security>();
        }

        var securities = await context.Securities
            .Where(c => tickerList.Contains(c.Ticker))
            .ToListAsync();

        return securities.ToDictionary(c => c.Ticker, c => c);
    }

    public async Task<List<Security>> GetByIdsAsync(IEnumerable<Guid> securityIds)
    {
        var securityIdList = securityIds.ToList();
        if (securityIdList.Count == 0)
        {
            return new List<Security>();
        }

        return await context.Securities
            .Where(c => securityIdList.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<Security> AddOrUpdateAsync(Security security)
    {
        // Find by Ticker (since Ticker has unique index)
        var existing = await context.Securities.FirstOrDefaultAsync(c => c.Ticker == security.Ticker);

        if (existing != null)
        {
            existing.CompanyName = security.CompanyName;
            existing.LastUpdated = DateTime.UtcNow;
            context.Securities.Update(existing);
        }
        else
        {
            // Generate Id if not set
            if (security.Id == Guid.Empty)
            {
                security.Id = Guid.NewGuid();
            }
            security.LastUpdated = DateTime.UtcNow;
            await context.Securities.AddAsync(security);
        }

        await context.SaveChangesAsync();
        return existing ?? security;
    }

    public async Task<IEnumerable<Security>> GetAllAsync()
    {
        return await context.Securities.ToListAsync();
    }

    public async Task<bool> DeleteAsync(string ticker)
    {
        var security = await GetByTickerAsync(ticker);
        if (security == null)
        {
            return false;
        }

        context.Securities.Remove(security);
        await context.SaveChangesAsync();
        return true;
    }
}

