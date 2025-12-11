using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class PriceFetchingService(
    IMarketPriceRepository marketPriceRepository,
    YahooFinanceService yahooFinanceService,
    ILogger<PriceFetchingService> logger)
{
    // Yahoo Finance rate limiting - be very conservative
    private const int MaxApiCallsPerRun = 10;
    // Delay between calls to avoid rate limiting (10 seconds)
    private const int DelayBetweenCallsSeconds = 10;
    // Cache prices for 30 minutes to reduce API calls
    private static readonly TimeSpan MaxPriceAge = TimeSpan.FromMinutes(30);

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

                // Fetch price from Yahoo Finance
                try
                {
                    var price = await yahooFinanceService.GetCurrentPriceAsync(ticker);

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
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Ticker not found"))
                {
                    // Ticker not found - mark it with a far-future timestamp so it won't be retried
                    await marketPriceRepository.MarkTickerAsNotFoundAsync(ticker);
                    logger.LogWarning("Ticker {Ticker} not found in Yahoo Finance. Marking as invalid and skipping future attempts.", ticker);
                    // Don't increment apiCallsMade since this wasn't a successful call
                    // The ticker will be skipped in future runs because of the far-future timestamp
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("rate limited"))
                {
                    // Yahoo Finance rate limited us - stop immediately and wait for next run
                    logger.LogWarning("Rate limited by Yahoo Finance. Stopping price fetching. Processed {Processed} tickers. Will resume in next run.", apiCallsMade);
                    break;
                }

                // Delay of 10 seconds between calls to give Yahoo Finance API time to recover (except for the last one)
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

