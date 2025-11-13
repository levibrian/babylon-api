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
                // Check for error in chart first (indicates ticker not found or invalid)
                if (chart.TryGetProperty("error", out var error))
                {
                    var errorText = error.GetRawText();
                    logger.LogWarning("Yahoo Finance API error for {Ticker}: {Error}. Ticker not found or invalid - will skip in future runs.", ticker, errorText);
                    throw new InvalidOperationException($"Ticker not found: {ticker}");
                }

                // Check if result array is empty (ticker not found)
                if (chart.TryGetProperty("result", out var result))
                {
                    if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() == 0)
                    {
                        logger.LogWarning("Yahoo Finance returned empty result for {Ticker}. Ticker not found - will skip in future runs.", ticker);
                        throw new InvalidOperationException($"Ticker not found: {ticker}");
                    }

                    if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0)
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

