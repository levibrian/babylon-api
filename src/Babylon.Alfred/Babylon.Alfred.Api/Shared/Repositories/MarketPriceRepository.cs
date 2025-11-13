using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class MarketPriceRepository(BabylonDbContext context) : IMarketPriceRepository
{
    public async Task<MarketPrice?> GetByTickerAsync(string ticker)
    {
        return await context.MarketPrices
            .Where(mp => mp.Ticker == ticker)
            .OrderByDescending(mp => mp.LastUpdated)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, MarketPrice>> GetByTickersAsync(IEnumerable<string> tickers)
    {
        var tickerList = tickers.ToList();
        if (tickerList.Count == 0)
        {
            return new Dictionary<string, MarketPrice>();
        }

        // Get the latest price for each ticker
        var prices = await context.MarketPrices
            .Where(mp => tickerList.Contains(mp.Ticker))
            .GroupBy(mp => mp.Ticker)
            .Select(g => g.OrderByDescending(mp => mp.LastUpdated).First())
            .ToListAsync();

        return prices.ToDictionary(mp => mp.Ticker, mp => mp);
    }

    public async Task UpsertMarketPriceAsync(string ticker, decimal price)
    {
        var existing = await context.MarketPrices
            .Where(mp => mp.Ticker == ticker)
            .OrderByDescending(mp => mp.LastUpdated)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            // Update existing (we only keep latest, so update the most recent one)
            existing.Price = price;
            existing.LastUpdated = DateTime.UtcNow;
            context.MarketPrices.Update(existing);
        }
        else
        {
            // Create new
            var marketPrice = new MarketPrice
            {
                Id = Guid.NewGuid(),
                Ticker = ticker,
                Price = price,
                LastUpdated = DateTime.UtcNow
            };
            await context.MarketPrices.AddAsync(marketPrice);
        }

        await context.SaveChangesAsync();
    }

    public async Task MarkTickerAsNotFoundAsync(string ticker)
    {
        // Mark ticker as "not found" by storing a price of 0 with a far-future timestamp
        // This ensures GetTickersNeedingUpdateAsync won't pick it up again
        var existing = await context.MarketPrices
            .Where(mp => mp.Ticker == ticker)
            .OrderByDescending(mp => mp.LastUpdated)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            // Update existing with far-future timestamp
            existing.Price = 0;
            existing.LastUpdated = DateTime.UtcNow.AddYears(100); // Far future - won't be picked up
            context.MarketPrices.Update(existing);
        }
        else
        {
            // Create new with far-future timestamp
            var marketPrice = new MarketPrice
            {
                Id = Guid.NewGuid(),
                Ticker = ticker,
                Price = 0,
                LastUpdated = DateTime.UtcNow.AddYears(100) // Far future - won't be picked up
            };
            await context.MarketPrices.AddAsync(marketPrice);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<string>> GetTickersNeedingUpdateAsync(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(maxAge);

        // Get all distinct tickers from allocation strategies (desired portfolio)
        // This includes securities users want to own, even if they haven't bought them yet
        var allTickers = await context.AllocationStrategies
            .Include(s => s.Security)
            .Where(s => s.Security != null)
            .Select(s => s.Security!.Ticker)
            .Distinct()
            .ToListAsync();

        if (allTickers.Count == 0)
        {
            return new List<string>();
        }

        // Get tickers that either don't exist or are older than maxAge
        var tickersNeedingUpdate = await context.MarketPrices
            .Where(mp => allTickers.Contains(mp.Ticker))
            .GroupBy(mp => mp.Ticker)
            .Select(g => new { Ticker = g.Key, LastUpdated = g.Max(mp => mp.LastUpdated) })
            .Where(x => x.LastUpdated < cutoffTime)
            .Select(x => x.Ticker)
            .ToListAsync();

        // Add tickers that don't have any price record
        var tickersWithPrices = await context.MarketPrices
            .Select(mp => mp.Ticker)
            .Distinct()
            .ToListAsync();

        var tickersWithoutPrices = allTickers.Except(tickersWithPrices).ToList();

        return tickersNeedingUpdate.Concat(tickersWithoutPrices).Distinct().ToList();
    }
}

