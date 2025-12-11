namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;

/// <summary>
/// Service for fetching historical price data from Yahoo Finance.
/// </summary>
public interface IHistoricalPriceService
{
    /// <summary>
    /// Gets historical daily prices for a ticker within a date range.
    /// </summary>
    /// <param name="ticker">Ticker symbol</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Dictionary of date to closing price</returns>
    Task<Dictionary<DateTime, decimal>> GetHistoricalPricesAsync(string ticker, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Gets current market prices for multiple tickers using the chart endpoint.
    /// More reliable than /v7/finance/quote as it doesn't require crumb authentication.
    /// </summary>
    /// <param name="tickers">Ticker symbols</param>
    /// <returns>Dictionary of ticker to current price</returns>
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> tickers);
}

