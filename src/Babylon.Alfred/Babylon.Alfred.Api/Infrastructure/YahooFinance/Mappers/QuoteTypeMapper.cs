using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Mappers;

/// <summary>
/// Maps Yahoo Finance quoteType strings to SecurityType enum values.
/// </summary>
public static class QuoteTypeMapper
{
    /// <summary>
    /// Converts Yahoo Finance quoteType to SecurityType enum.
    /// </summary>
    /// <param name="quoteType">Yahoo Finance quoteType (e.g., "EQUITY", "ETF", "CRYPTOCURRENCY")</param>
    /// <returns>Corresponding SecurityType enum value</returns>
    public static SecurityType ToSecurityType(string? quoteType)
    {
        if (string.IsNullOrWhiteSpace(quoteType))
            return SecurityType.Stock;

        return quoteType.ToUpperInvariant() switch
        {
            "EQUITY" => SecurityType.Stock,
            "ETF" => SecurityType.ETF,
            "CRYPTOCURRENCY" => SecurityType.Crypto,
            "MUTUALFUND" => SecurityType.MutualFund,
            "BOND" => SecurityType.Bond,
            "REIT" => SecurityType.REIT,
            "OPTION" => SecurityType.Options,
            "COMMODITY" => SecurityType.Commodity,
            _ => SecurityType.Stock // Default to Stock for unknown types
        };
    }
}
