using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for reading market prices from the database.
/// Prices are fetched by the background worker, this service only reads from DB.
/// </summary>
public class MarketPriceService : IMarketPriceService
{
    private readonly IMarketPriceRepository _marketPriceRepository;

    public MarketPriceService(IMarketPriceRepository marketPriceRepository)
    {
        _marketPriceRepository = marketPriceRepository;
    }

    public async Task<decimal?> GetCurrentPriceAsync(string ticker)
    {
        var marketPrice = await _marketPriceRepository.GetByTickerAsync(ticker);
        return marketPrice?.Price;
    }

    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> tickers)
    {
        var marketPrices = await _marketPriceRepository.GetByTickersAsync(tickers);
        return marketPrices.ToDictionary(mp => mp.Key, mp => mp.Value.Price);
    }
}

