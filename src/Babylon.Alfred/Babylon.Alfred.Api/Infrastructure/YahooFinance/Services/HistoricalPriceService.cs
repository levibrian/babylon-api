using System.Text.Json;

namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;

/// <summary>
/// Service for fetching historical price data from Yahoo Finance using the chart API.
/// </summary>
public class HistoricalPriceService(HttpClient httpClient, ILogger<HistoricalPriceService> logger) : IHistoricalPriceService
{
    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

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

        if (chartResult.TryGetProperty("timestamp", out var timestampElement) &&
            chartResult.TryGetProperty("indicators", out var indicators) &&
            indicators.TryGetProperty("quote", out var quote) &&
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

