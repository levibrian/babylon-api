using System.Text.Json;

namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;

/// <summary>
/// Service for fetching historical price data from Yahoo Finance using the chart API.
/// </summary>
public class HistoricalPriceService(HttpClient httpClient, ILogger<HistoricalPriceService> logger) : IHistoricalPriceService
{
    // Using query2 instead of query1 - often less rate-limited
    // Reference: https://github.com/Scarvy/yahoo-finance-api-collection
    private const string BaseUrl = "https://query2.finance.yahoo.com/v8/finance/chart";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private const int RequestDelayMs = 100; // Small delay to avoid rate limiting

    /// <inheritdoc />
    public async Task<Dictionary<DateTime, decimal>> GetHistoricalPricesAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);

        try
        {
            var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeSeconds();
            var endTimestamp = new DateTimeOffset(endDate).ToUnixTimeSeconds();

            var url = $"{BaseUrl}/{ticker}?interval=1d&period1={startTimestamp}&period2={endTimestamp}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", UserAgent);

            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to fetch historical prices for {Ticker}: {StatusCode}",
                    ticker,
                    response.StatusCode);
                return new Dictionary<DateTime, decimal>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return ParsePricesFromJson(content, ticker);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching historical prices for {Ticker}", ticker);
            return new Dictionary<DateTime, decimal>();
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> tickers)
    {
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var tickerList = tickers.ToList();

        foreach (var ticker in tickerList)
        {
            try
            {
                // Use range=1d to get current price from meta.regularMarketPrice
                var url = $"{BaseUrl}/{ticker}?interval=1d&range=1d";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", UserAgent);

                using var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var price = ParseCurrentPriceFromJson(content);
                    if (price > 0)
                    {
                        prices[ticker] = price;
                    }
                }
                else
                {
                    logger.LogWarning(
                        "Failed to fetch current price for {Ticker}: {StatusCode}",
                        ticker,
                        response.StatusCode);
                }

                // Small delay between requests to avoid rate limiting
                if (tickerList.IndexOf(ticker) < tickerList.Count - 1)
                {
                    await Task.Delay(RequestDelayMs);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching current price for {Ticker}", ticker);
            }
        }

        return prices;
    }

    private decimal ParseCurrentPriceFromJson(string json)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(json);

            if (jsonDoc.RootElement.TryGetProperty("chart", out var chart) &&
                chart.TryGetProperty("result", out var resultArray) &&
                resultArray.ValueKind == JsonValueKind.Array &&
                resultArray.GetArrayLength() > 0)
            {
                var firstResult = resultArray[0];
                if (firstResult.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("regularMarketPrice", out var price))
                {
                    return price.GetDecimal();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse current price from JSON");
        }

        return 0;
    }

    private Dictionary<DateTime, decimal> ParsePricesFromJson(string json, string ticker)
    {
        var prices = new Dictionary<DateTime, decimal>();

        using var jsonDoc = JsonDocument.Parse(json);

        if (!TryGetChartResult(jsonDoc.RootElement, out var firstResult))
        {
            logger.LogWarning("No chart result found for {Ticker}", ticker);
            return prices;
        }

        if (!TryGetPriceArrays(firstResult, out var timestamps, out var closes))
        {
            logger.LogWarning("No price data found for {Ticker}", ticker);
            return prices;
        }

        for (int i = 0; i < Math.Min(timestamps.Count, closes.Count); i++)
        {
            if (TryParsePrice(timestamps[i], closes[i], out var date, out var price))
            {
                prices[date] = price;
            }
        }

        return prices;
    }

    private static bool TryGetChartResult(JsonElement root, out JsonElement result)
    {
        result = default;

        if (root.TryGetProperty("chart", out var chart) &&
            chart.TryGetProperty("result", out var resultArray) &&
            resultArray.ValueKind == JsonValueKind.Array &&
            resultArray.GetArrayLength() > 0)
        {
            result = resultArray[0];
            return true;
        }

        return false;
    }

    private static bool TryGetPriceArrays(
        JsonElement chartResult,
        out List<JsonElement> timestamps,
        out List<JsonElement> closes)
    {
        timestamps = new List<JsonElement>();
        closes = new List<JsonElement>();

        if (!chartResult.TryGetProperty("timestamp", out var timestampElement) ||
            !chartResult.TryGetProperty("indicators", out var indicators))
        {
            return false;
        }

        // Prefer adjusted close for historical analysis (accounts for splits/dividends)
        // Path: indicators.adjclose[0].adjclose
        if (indicators.TryGetProperty("adjclose", out var adjclose) &&
            adjclose.ValueKind == JsonValueKind.Array &&
            adjclose.GetArrayLength() > 0 &&
            adjclose[0].TryGetProperty("adjclose", out var adjcloseElement))
        {
            timestamps = timestampElement.EnumerateArray().ToList();
            closes = adjcloseElement.EnumerateArray().ToList();
            return true;
        }

        // Fallback to regular close if adjclose not available
        // Path: indicators.quote[0].close
        if (indicators.TryGetProperty("quote", out var quote) &&
            quote.ValueKind == JsonValueKind.Array &&
            quote.GetArrayLength() > 0 &&
            quote[0].TryGetProperty("close", out var closeElement))
        {
            timestamps = timestampElement.EnumerateArray().ToList();
            closes = closeElement.EnumerateArray().ToList();
            return true;
        }

        return false;
    }

    private static bool TryParsePrice(
        JsonElement timestamp,
        JsonElement close,
        out DateTime date,
        out decimal price)
    {
        date = default;
        price = default;

        if (timestamp.ValueKind != JsonValueKind.Number ||
            close.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        var closePrice = close.GetDecimal();
        if (closePrice <= 0)
        {
            return false;
        }

        date = DateTimeOffset.FromUnixTimeSeconds(timestamp.GetInt64()).UtcDateTime.Date;
        price = closePrice;
        return true;
    }
}

