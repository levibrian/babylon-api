using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class MarketPriceRepository(BabylonDbContext context, ILogger<MarketPriceRepository> logger) : IMarketPriceRepository
{
    public async Task<MarketPrice?> GetByTickerAsync(string ticker)
    {
        return await context.MarketPrices
            .Include(mp => mp.Security)
            .Where(mp => mp.Security.Ticker == ticker)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, MarketPrice>> GetByTickersAsync(IEnumerable<string> tickers)
    {
        var tickerList = tickers.ToList();
        if (tickerList.Count == 0)
        {
            return new Dictionary<string, MarketPrice>();
        }

        var prices = await context.MarketPrices
            .Include(mp => mp.Security)
            .Where(mp => tickerList.Contains(mp.Security.Ticker))
            .ToListAsync();

        return prices.ToDictionary(mp => mp.Security.Ticker, mp => mp);
    }

    public async Task UpsertMarketPriceAsync(Guid securityId, decimal price, string? currency = null)
    {
        logger.LogDatabaseOperation("Upsert", "MarketPrice", new { SecurityId = securityId, Price = price });
        
        var existing = await context.MarketPrices
            .FirstOrDefaultAsync(mp => mp.SecurityId == securityId);

        if (existing != null)
        {
            existing.Price = price;
            existing.Currency = currency ?? existing.Currency;
            existing.LastUpdated = DateTime.UtcNow;
            context.MarketPrices.Update(existing);
            logger.LogDatabaseOperation("Updated", "MarketPrice", new { SecurityId = securityId, Price = price });
        }
        else
        {
            var marketPrice = new MarketPrice
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                Price = price,
                Currency = currency,
                LastUpdated = DateTime.UtcNow
            };
            await context.MarketPrices.AddAsync(marketPrice);
            logger.LogDatabaseOperation("Created", "MarketPrice", new { SecurityId = securityId, Price = price });
        }

        await context.SaveChangesAsync();
    }

    public async Task MarkSecurityAsNotFoundAsync(Guid securityId)
    {
        // Mark security as "not found" by storing a price of 0 with a far-future timestamp
        var existing = await context.MarketPrices
            .FirstOrDefaultAsync(mp => mp.SecurityId == securityId);

        if (existing != null)
        {
            existing.Price = 0;
            existing.LastUpdated = DateTime.UtcNow.AddYears(100);
            context.MarketPrices.Update(existing);
        }
        else
        {
            var marketPrice = new MarketPrice
            {
                Id = Guid.NewGuid(),
                SecurityId = securityId,
                Price = 0,
                LastUpdated = DateTime.UtcNow.AddYears(100)
            };
            await context.MarketPrices.AddAsync(marketPrice);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<Security>> GetSecuritiesNeedingUpdateAsync(TimeSpan maxAge)
    {
        logger.LogDatabaseOperation("GetSecuritiesNeedingUpdate", "MarketPrice", new { MaxAge = maxAge });
        
        var cutoffTime = DateTime.UtcNow.Subtract(maxAge);

        // Get all distinct securities from allocation strategies
        var securitiesInPortfolio = await context.AllocationStrategies
            .Include(s => s.Security)
            .Where(s => s.Security != null)
            .Select(s => s.Security!)
            .Distinct()
            .ToListAsync();

        if (securitiesInPortfolio.Count == 0)
        {
            logger.LogInformation("No securities found in allocation strategies");
            return new List<Security>();
        }

        var securityIds = securitiesInPortfolio.Select(s => s.Id).ToList();

        // Get securities that have stale prices
        var securitiesWithStalePrices = await context.MarketPrices
            .Where(mp => securityIds.Contains(mp.SecurityId) && mp.LastUpdated < cutoffTime)
            .Select(mp => mp.SecurityId)
            .ToListAsync();

        // Get securities that don't have any price record
        var securitiesWithPrices = await context.MarketPrices
            .Where(mp => securityIds.Contains(mp.SecurityId))
            .Select(mp => mp.SecurityId)
            .Distinct()
            .ToListAsync();

        var securitiesWithoutPrices = securityIds.Except(securitiesWithPrices).ToList();

        // Combine and get full Security objects
        var needsUpdate = securitiesWithStalePrices.Concat(securitiesWithoutPrices).Distinct().ToList();
        var result = securitiesInPortfolio.Where(s => needsUpdate.Contains(s.Id)).ToList();
        
        logger.LogDatabaseOperation("RetrievedSecuritiesNeedingUpdate", "MarketPrice", null, result.Count);
        
        return result;
    }
}
