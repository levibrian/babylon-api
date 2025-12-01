using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Validates security existence and retrieves security information.
/// </summary>
public static class SecurityValidator
{
    /// <summary>
    /// Validates that a security exists for the given ticker and returns it.
    /// </summary>
    /// <param name="ticker">The ticker symbol to validate</param>
    /// <param name="securityRepository">Repository to fetch security</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>The security entity if found</returns>
    /// <exception cref="InvalidOperationException">Thrown when security is not found</exception>
    public static async Task<Security> ValidateAndGetSecurityAsync(
        string ticker,
        ISecurityRepository securityRepository,
        ILogger logger)
    {
        var security = await securityRepository.GetByTickerAsync(ticker);
        
        if (security is null)
        {
            logger.LogBusinessRuleViolation("SecurityValidator", 
                $"Security not found for ticker: {ticker}", 
                new { Ticker = ticker });
            throw new InvalidOperationException(ErrorMessages.SecurityNotFound);
        }

        return security;
    }

    /// <summary>
    /// Validates that all tickers exist and returns a dictionary of securities.
    /// </summary>
    /// <param name="tickers">List of ticker symbols to validate</param>
    /// <param name="securityRepository">Repository to fetch securities</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Dictionary of ticker to Security</returns>
    /// <exception cref="InvalidOperationException">Thrown when any ticker is not found</exception>
    public static async Task<Dictionary<string, Security>> ValidateAndGetSecuritiesAsync(
        IEnumerable<string> tickers,
        ISecurityRepository securityRepository,
        ILogger logger)
    {
        var tickerList = tickers.Distinct().ToList();
        var securities = await securityRepository.GetByTickersAsync(tickerList);

        var missingTickers = tickerList.Where(t => !securities.ContainsKey(t)).ToList();
        if (missingTickers.Any())
        {
            logger.LogBusinessRuleViolation("SecurityValidator",
                $"Securities not found for tickers: {string.Join(", ", missingTickers)}",
                new { MissingTickers = missingTickers });
            throw new InvalidOperationException(
                string.Format(ErrorMessages.SecuritiesNotFoundForTickers, string.Join(", ", missingTickers)));
        }

        return securities;
    }
}

