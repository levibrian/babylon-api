using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class PriceFetchingService
{
    private readonly IMarketPriceRepository _marketPriceRepository;
    private readonly IAllocationStrategyRepository _allocationStrategyRepository;
    private readonly AlphaVantageService _alphaVantageService;
    private readonly ILogger<PriceFetchingService> _logger;
    private const int MaxApiCallsPerRun = 5;
    private const int DelayBetweenCallsSeconds = 12; // 5 calls per minute = 12 seconds between calls
    private static readonly TimeSpan MaxPriceAge = TimeSpan.FromMinutes(15);

    public PriceFetchingService(
        IMarketPriceRepository marketPriceRepository,
        IAllocationStrategyRepository allocationStrategyRepository,
        AlphaVantageService alphaVantageService,
        ILogger<PriceFetchingService> logger)
    {
        _marketPriceRepository = marketPriceRepository;
        _allocationStrategyRepository = allocationStrategyRepository;
        _alphaVantageService = alphaVantageService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting price fetching job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Get tickers that need price updates (not updated in last 15 minutes)
            var tickersNeedingUpdate = await _marketPriceRepository.GetTickersNeedingUpdateAsync(MaxPriceAge);

            if (tickersNeedingUpdate.Count == 0)
            {
                _logger.LogInformation("No tickers need price updates at {Timestamp}", DateTime.UtcNow);
                return;
            }

            _logger.LogInformation("Found {Count} tickers needing price updates", tickersNeedingUpdate.Count);

            var apiCallsMade = 0;
            var tickerIndex = 0;

            foreach (var ticker in tickersNeedingUpdate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Price fetching job cancelled");
                    break;
                }

                // Stop if we've reached the rate limit
                if (apiCallsMade >= MaxApiCallsPerRun)
                {
                    _logger.LogInformation("Reached rate limit ({MaxCalls} calls). Stopping. Remaining tickers will be processed in next run.", MaxApiCallsPerRun);
                    break;
                }

                // Fetch price from Alpha Vantage
                var price = await _alphaVantageService.GetCurrentPriceAsync(ticker);

                if (price.HasValue)
                {
                    // Upsert the price in database
                    await _marketPriceRepository.UpsertMarketPriceAsync(ticker, price.Value);
                    _logger.LogInformation("Updated price for {Ticker}: {Price}", ticker, price.Value);
                    apiCallsMade++;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch price for {Ticker}. Will retry in next run.", ticker);
                }

                // Wait before next call (except for the last one)
                tickerIndex++;
                if (apiCallsMade < MaxApiCallsPerRun && tickerIndex < tickersNeedingUpdate.Count)
                {
                    _logger.LogDebug("Waiting {Delay} seconds before next API call...", DelayBetweenCallsSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(DelayBetweenCallsSeconds), cancellationToken);
                }
            }

            _logger.LogInformation("Price fetching job completed. Processed {Processed} tickers out of {Total} needing updates at {Timestamp}",
                apiCallsMade, tickersNeedingUpdate.Count, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in price fetching job");
            throw;
        }
    }
}

