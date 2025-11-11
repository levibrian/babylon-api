using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IMarketPriceRepository
{
    Task<MarketPrice?> GetByTickerAsync(string ticker);
    Task<Dictionary<string, MarketPrice>> GetByTickersAsync(IEnumerable<string> tickers);
    Task UpsertMarketPriceAsync(string ticker, decimal price);
    Task<List<string>> GetTickersNeedingUpdateAsync(TimeSpan maxAge);
}

