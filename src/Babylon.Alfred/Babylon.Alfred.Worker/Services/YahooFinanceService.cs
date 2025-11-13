using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class YahooFinanceService(HttpClient httpClient, ILogger<YahooFinanceService> logger)
{
    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart";
    private static bool _sessionInitialized;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    private async Task EnsureSessionInitializedAsync()
    {
        if (_sessionInitialized) return;

        await InitLock.WaitAsync();
        try
        {
            if (_sessionInitialized) return;

            // Make an initial request to Yahoo Finance homepage to establish cookies/session
            // This mimics what yfinance does - establishing a session before API calls
            logger.LogInformation("Initializing Yahoo Finance session...");
            var initRequest = new HttpRequestMessage(HttpMethod.Get, "https://finance.yahoo.com/");
            await httpClient.SendAsync(initRequest);
            _sessionInitialized = true;
            logger.LogInformation("Yahoo Finance session initialized");
        }
        finally
        {
            InitLock.Release();
        }
    }

    public async Task<decimal?> GetCurrentPriceAsync(string ticker)
    {
        try
        {
            // Ensure session is initialized (like yfinance does)
            await EnsureSessionInitializedAsync();

            // Add query parameters that browsers typically send
            var url = $"{BaseUrl}/{ticker}?interval=1d&range=1d";
            logger.LogInformation("Fetching price for {Ticker} from Yahoo Finance at {Timestamp}", ticker, DateTime.UtcNow);

            // Use HttpRequestMessage for more control over the request
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Yahoo Finance API returned {StatusCode} for {Ticker}. Response: {Response}",
                    response.StatusCode, ticker, errorContent.Substring(0, Math.Min(500, errorContent.Length)));

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    logger.LogError("Rate limited by Yahoo Finance. This may indicate bot detection.");
                }

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

