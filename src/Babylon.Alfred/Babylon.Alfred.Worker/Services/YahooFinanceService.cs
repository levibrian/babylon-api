using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

/// <summary>
/// Result from fetching a price from Yahoo Finance.
/// </summary>
public record YahooPriceResult(decimal Price, string? Currency);

/// <summary>
/// Service for fetching prices from Yahoo Finance using the chart API.
/// </summary>
public class YahooFinanceService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly ILogger<YahooFinanceService> logger;

    // Same User-Agent as YahooMarketDataService in the API project
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private const string BaseUrl = "https://query2.finance.yahoo.com/v8/finance/chart";

    public YahooFinanceService(HttpClient httpClient, ILogger<YahooFinanceService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set User-Agent in constructor (same pattern as YahooMarketDataService)
        if (this.httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }
    }

    public void Dispose()
    {
        // HttpClient is managed by DI, don't dispose it
    }

    /// <summary>
    /// Fetches the current price and currency for a ticker.
    /// Uses simple GetAsync like the SearchAsync method does.
    /// </summary>
    public async Task<YahooPriceResult?> GetCurrentPriceAsync(string ticker)
    {
        try
        {
            var url = $"{BaseUrl}/{ticker}?interval=1d&range=1d";
            logger.LogDebug("Fetching price for {Ticker}", ticker);

            // Use simple GetAsync - inherits all default headers from HttpClient
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Yahoo Finance returned {StatusCode} for {Ticker}",
                    response.StatusCode,
                    ticker);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Yahoo Finance rate limited");
                }

                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return ParsePriceFromJson(content, ticker);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching price for {Ticker}", ticker);
            return null;
        }
    }

    private YahooPriceResult? ParsePriceFromJson(string json, string ticker)
    {
        using var jsonDoc = JsonDocument.Parse(json);

        if (!jsonDoc.RootElement.TryGetProperty("chart", out var chart))
        {
            logger.LogWarning("No chart property for {Ticker}", ticker);
            return null;
        }

        // Check for error
        if (chart.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
        {
            logger.LogWarning("API error for {Ticker}: {Error}", ticker, error.GetRawText());
            throw new InvalidOperationException($"Ticker not found: {ticker}");
        }

        if (!chart.TryGetProperty("result", out var result) ||
            result.ValueKind != JsonValueKind.Array ||
            result.GetArrayLength() == 0)
        {
            logger.LogWarning("Empty result for {Ticker}", ticker);
            throw new InvalidOperationException($"Ticker not found: {ticker}");
        }

        var firstResult = result[0];
        if (!firstResult.TryGetProperty("meta", out var meta))
        {
            logger.LogWarning("No meta for {Ticker}", ticker);
            return null;
        }

        // Extract currency
        string? currency = null;
        if (meta.TryGetProperty("currency", out var currencyElement) &&
            currencyElement.ValueKind == JsonValueKind.String)
        {
            currency = currencyElement.GetString();
        }

        // Try regularMarketPrice first, then previousClose
        if (meta.TryGetProperty("regularMarketPrice", out var priceElement) &&
            priceElement.ValueKind == JsonValueKind.Number)
        {
            var price = priceElement.GetDecimal();
            logger.LogDebug("Got price for {Ticker}: {Price} {Currency}", ticker, price, currency);
            return new YahooPriceResult(price, currency);
        }

        if (meta.TryGetProperty("previousClose", out var prevCloseElement) &&
            prevCloseElement.ValueKind == JsonValueKind.Number)
        {
            var price = prevCloseElement.GetDecimal();
            logger.LogDebug("Got previousClose for {Ticker}: {Price} {Currency}", ticker, price, currency);
            return new YahooPriceResult(price, currency);
        }

        logger.LogWarning("No price data for {Ticker}", ticker);
        return null;
    }
}
