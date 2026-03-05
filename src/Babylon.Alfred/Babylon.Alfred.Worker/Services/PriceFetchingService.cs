using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

public class PriceFetchingService(
    IMarketPriceRepository marketPriceRepository,
    YahooFinanceService yahooFinanceService,
    ILogger<PriceFetchingService> logger)
{
    // Rate limiting settings - job runs hourly during market hours
    private const int MaxApiCallsPerRun = 50;
    private const int DelayBetweenCallsSeconds = 3;
    private const int MaxRetries = 3;
    private static readonly TimeSpan MaxPriceAge = TimeSpan.FromHours(1);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogInformation("PriceFetchingService starting");

        try
        {
            var securitiesNeedingUpdate = await marketPriceRepository.GetSecuritiesNeedingUpdateAsync(MaxPriceAge);

            if (securitiesNeedingUpdate.Count == 0)
            {
                logger.LogDebug("No securities need price updates — skipping");
                return;
            }

            logger.LogInformation("Found {Count} securities needing price updates", securitiesNeedingUpdate.Count);

            var processed = 0;
            var rateLimitHits = 0;

            foreach (var security in securitiesNeedingUpdate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation(
                        "Cancellation requested — {Processed}/{Total} securities processed",
                        processed, securitiesNeedingUpdate.Count);
                    break;
                }

                if (processed >= MaxApiCallsPerRun)
                {
                    var remaining = securitiesNeedingUpdate.Count - processed;
                    logger.LogInformation(
                        "Reached max calls ({MaxCalls}) — {Remaining} remaining securities will be processed in next run",
                        MaxApiCallsPerRun, remaining);
                    break;
                }

                // Add delay before each call (except first)
                if (processed > 0)
                {
                    var delay = DelayBetweenCallsSeconds + rateLimitHits * 5; // Increase delay after rate limits
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                }

                var success = await FetchAndStorePriceWithRetryAsync(security, cancellationToken);

                if (!success)
                {
                    rateLimitHits++;
                    if (rateLimitHits >= 3)
                    {
                        logger.LogWarning(
                            "Too many rate limit hits ({RateLimitHits}) — stopping early after {Processed}/{Total} securities",
                            rateLimitHits, processed, securitiesNeedingUpdate.Count);
                        break;
                    }
                }

                processed++;
            }

            var elapsed = DateTime.UtcNow - startedAt;
            logger.LogInformation(
                "PriceFetchingService complete — {Processed}/{Total} securities processed in {ElapsedMs}ms",
                processed, securitiesNeedingUpdate.Count, (int)elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in price fetching job");
            throw;
        }
    }

    private async Task<bool> FetchAndStorePriceWithRetryAsync(Security security, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var result = await yahooFinanceService.GetCurrentPriceAsync(security.Ticker);

                if (result != null)
                {
                    var currency = result.Currency ?? security.Currency;

                    await marketPriceRepository.UpsertMarketPriceAsync(
                        security.Id,
                        result.Price,
                        currency);

                    logger.LogDebug(
                        "Updated price for {Ticker}: {Price} {Currency}",
                        security.Ticker, result.Price, currency);
                    return true;
                }

                logger.LogWarning("No price data for {Ticker}", security.Ticker);
                return true; // Don't retry if no data (vs rate limit)
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Ticker not found"))
            {
                await marketPriceRepository.MarkSecurityAsNotFoundAsync(security.Id);
                logger.LogWarning("Ticker {Ticker} not found. Marking as invalid.", security.Ticker);
                return true; // Don't retry
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("rate limited"))
            {
                var backoffSeconds = (int)Math.Pow(2, attempt) * 5; // 10s, 20s, 40s
                logger.LogWarning(
                    "Rate limited on attempt {Attempt}/{MaxRetries} for {Ticker}. Waiting {Backoff}s",
                    attempt, MaxRetries, security.Ticker, backoffSeconds);

                if (attempt < MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancellationToken);
                }
                else
                {
                    return false; // Exhausted retries
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching price for {Ticker}", security.Ticker);
                return true; // Don't retry on unknown errors
            }
        }

        return false;
    }
}
