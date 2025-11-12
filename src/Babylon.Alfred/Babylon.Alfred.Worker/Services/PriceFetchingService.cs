using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class PriceFetchingService(
    IMarketPriceRepository marketPriceRepository,
    AlphaVantageService alphaVantageService,
    ILogger<PriceFetchingService> logger)
{
    private const int MaxApiCallsPerRun = 5;
    private const int DelayBetweenCallsSeconds = 12; // 5 calls per minute = 12 seconds between calls
    private static readonly TimeSpan MaxPriceAge = TimeSpan.FromMinutes(15);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting price fetching job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Get tickers that need price updates (not updated in last 15 minutes)
            var tickersNeedingUpdate = await marketPriceRepository.GetTickersNeedingUpdateAsync(MaxPriceAge);

            if (tickersNeedingUpdate.Count == 0)
            {
                logger.LogInformation("No tickers need price updates at {Timestamp}", DateTime.UtcNow);
                return;
            }

            logger.LogInformation("Found {Count} tickers needing price updates", tickersNeedingUpdate.Count);

            var apiCallsMade = 0;
            var tickerIndex = 0;

            foreach (var ticker in tickersNeedingUpdate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Price fetching job cancelled");
                    break;
                }

                // Stop if we've reached the rate limit
                if (apiCallsMade >= MaxApiCallsPerRun)
                {
                    logger.LogInformation("Reached rate limit ({MaxCalls} calls). Stopping. Remaining tickers will be processed in next run", MaxApiCallsPerRun);
                    break;
                }

                // Fetch price from Alpha Vantage
                var price = await alphaVantageService.GetCurrentPriceAsync(ticker);

                if (price.HasValue)
                {
                    // Upsert the price in database
                    await marketPriceRepository.UpsertMarketPriceAsync(ticker, price.Value);
                    logger.LogInformation("Updated price for {Ticker}: {Price}", ticker, price.Value);
                    apiCallsMade++;
                }
                else
                {
                    logger.LogWarning("Failed to fetch price for {Ticker}. Will retry in next run", ticker);
                }

                // Wait before next call (except for the last one)
                tickerIndex++;
                if (apiCallsMade < MaxApiCallsPerRun && tickerIndex < tickersNeedingUpdate.Count)
                {
                    logger.LogDebug("Waiting {Delay} seconds before next API call...", DelayBetweenCallsSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(DelayBetweenCallsSeconds), cancellationToken);
                }
            }

            logger.LogInformation("Price fetching job completed. Processed {Processed} tickers out of {Total} needing updates at {Timestamp}",
                apiCallsMade, tickersNeedingUpdate.Count, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in price fetching job");
            throw;
        }
    }
}

