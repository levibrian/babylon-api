using System.Text.Json;
using Babylon.Alfred.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class AlphaVantageService(HttpClient httpClient, ILogger<AlphaVantageService> logger)
{
    private const string ApiKey = "0WKAG6P6F24DEVD6";
    private const string BaseUrl = "https://www.alphavantage.co/query";

    public async Task<decimal?> GetCurrentPriceAsync(string ticker)
    {
        try
        {
            var url = $"{BaseUrl}?function=GLOBAL_QUOTE&symbol={ticker}&apikey={ApiKey}";
            logger.LogInformation("Fetching price for {Ticker} from Alpha Vantage at {Timestamp}", ticker, DateTime.UtcNow);

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            // Alpha Vantage returns data in "Global Quote" object
            if (jsonDoc.RootElement.TryGetProperty("Global Quote", out var globalQuote))
            {
                if (globalQuote.TryGetProperty("05. price", out var priceElement))
                {
                    if (decimal.TryParse(priceElement.GetString(), out var price))
                    {
                        logger.LogInformation("Successfully fetched price for {Ticker}: {Price} at {Timestamp}", ticker, price, DateTime.UtcNow);
                        return price;
                    }
                }
            }

            // Check for error messages
            if (jsonDoc.RootElement.TryGetProperty("Error Message", out var errorMessage))
            {
                logger.LogWarning("Alpha Vantage API error for {Ticker}: {Error}", ticker, errorMessage.GetString());
                return null;
            }

            if (jsonDoc.RootElement.TryGetProperty("Note", out var note))
            {
                logger.LogWarning("Alpha Vantage API rate limit notice for {Ticker}: {Note}", ticker, note.GetString());
                return null;
            }

            logger.LogWarning("Unexpected response format for {Ticker}", ticker);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching price for {Ticker} from Alpha Vantage", ticker);
            return null;
        }
    }
}

