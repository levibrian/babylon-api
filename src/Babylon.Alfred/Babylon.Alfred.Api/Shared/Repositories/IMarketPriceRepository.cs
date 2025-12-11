using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IMarketPriceRepository
{
    /// <summary>
    /// Gets market price by ticker (looks up via Security).
    /// </summary>
    Task<MarketPrice?> GetByTickerAsync(string ticker);
    
    /// <summary>
    /// Gets market prices by tickers (looks up via Security).
    /// Returns dictionary keyed by ticker for easy lookup.
    /// </summary>
    Task<Dictionary<string, MarketPrice>> GetByTickersAsync(IEnumerable<string> tickers);
    
    /// <summary>
    /// Upserts a market price for a security.
    /// </summary>
    Task UpsertMarketPriceAsync(Guid securityId, decimal price, string? currency = null);
    
    /// <summary>
    /// Marks a security as "not found" so it won't be retried.
    /// </summary>
    Task MarkSecurityAsNotFoundAsync(Guid securityId);
    
    /// <summary>
    /// Gets securities that need price updates.
    /// Returns Security objects (with Ticker for Yahoo lookup).
    /// </summary>
    Task<List<Security>> GetSecuritiesNeedingUpdateAsync(TimeSpan maxAge);
}

