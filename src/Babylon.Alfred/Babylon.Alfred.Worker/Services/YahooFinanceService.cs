using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class YahooFinanceService(HttpClient httpClient, ILogger<YahooFinanceService> logger)
{
    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart";

    public async Task<decimal?> GetCurrentPriceAsync(string ticker)
    {
        try
        {
            var url = $"{BaseUrl}/{ticker}";
            logger.LogInformation("Fetching price for {Ticker} from Yahoo Finance at {Timestamp}", ticker, DateTime.UtcNow);

            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Yahoo Finance API returned {StatusCode} for {Ticker}", response.StatusCode, ticker);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            // Yahoo Finance returns data in chart.result array
            if (jsonDoc.RootElement.TryGetProperty("chart", out var chart))
            {
                if (chart.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0)
                {
                    var firstResult = result[0];
                    
                    // Get price from meta.previousClose (previous day's closing price)
                    // This is appropriate for portfolio balance calculations as it uses stable end-of-day prices
                    if (firstResult.TryGetProperty("meta", out var meta))
                    {
                        // Prioritize previousClose for portfolio valuation (stable end-of-day price)
                        if (meta.TryGetProperty("previousClose", out var previousCloseElement))
                        {
                            if (previousCloseElement.ValueKind == JsonValueKind.Number && previousCloseElement.TryGetDecimal(out var price))
                            {
                                logger.LogInformation("Successfully fetched previous close price for {Ticker}: {Price} at {Timestamp}", ticker, price, DateTime.UtcNow);
                                return price;
                            }
                        }
                        
                        // Fallback to regularMarketPrice if previousClose is not available
                        if (meta.TryGetProperty("regularMarketPrice", out var priceElement))
                        {
                            if (priceElement.ValueKind == JsonValueKind.Number && priceElement.TryGetDecimal(out var price))
                            {
                                logger.LogInformation("Using regularMarketPrice as fallback for {Ticker}: {Price} at {Timestamp}", ticker, price, DateTime.UtcNow);
                                return price;
                            }
                        }
                        
                        logger.LogWarning("No price data available for {Ticker}. previousClose and regularMarketPrice both unavailable.", ticker);
                        return null;
                    }
                }
                
                // Check for error in chart
                if (chart.TryGetProperty("error", out var error))
                {
                    logger.LogWarning("Yahoo Finance API error for {Ticker}: {Error}", ticker, error.GetRawText());
                    return null;
                }
            }

            logger.LogWarning("Unexpected response format for {Ticker}. Response: {Response}", ticker, content.Substring(0, Math.Min(500, content.Length)));
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching price for {Ticker} from Yahoo Finance", ticker);
            return null;
        }
    }
}

