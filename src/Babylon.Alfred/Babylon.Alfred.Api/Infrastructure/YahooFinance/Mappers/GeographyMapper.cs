namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Mappers;

/// <summary>
/// Maps exchange codes and currency symbols to geographic regions.
/// Used for portfolio diversification analysis.
/// </summary>
public static class GeographyMapper
{
    /// <summary>
    /// Converts exchange code or currency to a standardized geography/region.
    /// </summary>
    /// <param name="exchange">Exchange code (e.g., "NMS", "NYQ", "LSE", "TYO")</param>
    /// <param name="currency">Currency code (e.g., "USD", "EUR", "JPY")</param>
    /// <returns>Geographic region string</returns>
    public static string? ToGeography(string? exchange, string? currency)
    {
        // First, try to determine geography from exchange code
        if (!string.IsNullOrWhiteSpace(exchange))
        {
            var exchangeUpper = exchange.ToUpperInvariant();
            
            // North American exchanges
            if (exchangeUpper.Contains("NMS") ||    // NASDAQ
                exchangeUpper.Contains("NYQ") ||    // NYSE
                exchangeUpper.Contains("NYS") ||
                exchangeUpper.Contains("PCX") ||    // NYSE Arca
                exchangeUpper.Contains("TSE") ||    // Toronto Stock Exchange
                exchangeUpper.Contains("TSX"))
            {
                return "North America";
            }
            
            // European exchanges
            if (exchangeUpper.Contains("LSE") ||    // London Stock Exchange
                exchangeUpper.Contains("FRA") ||    // Frankfurt
                exchangeUpper.Contains("GER") ||    // XETRA (Germany)
                exchangeUpper.Contains("PAR") ||    // Euronext Paris
                exchangeUpper.Contains("AMS") ||    // Euronext Amsterdam
                exchangeUpper.Contains("SWX") ||    // SIX Swiss Exchange
                exchangeUpper.Contains("MIL"))      // Borsa Italiana
            {
                return "Europe";
            }
            
            // Asian exchanges
            if (exchangeUpper.Contains("TYO") ||    // Tokyo
                exchangeUpper.Contains("HKG") ||    // Hong Kong
                exchangeUpper.Contains("SHG") ||    // Shanghai
                exchangeUpper.Contains("SHE") ||    // Shenzhen
                exchangeUpper.Contains("KSC") ||    // Korea Stock Exchange
                exchangeUpper.Contains("SES") ||    // Singapore
                exchangeUpper.Contains("BSE") ||    // Bombay Stock Exchange
                exchangeUpper.Contains("NSE"))      // National Stock Exchange of India
            {
                return "Asia";
            }
        }
        
        // Fallback to currency-based mapping
        if (!string.IsNullOrWhiteSpace(currency))
        {
            return currency.ToUpperInvariant() switch
            {
                "USD" or "CAD" or "MXN" => "North America",
                "EUR" or "GBP" or "CHF" or "SEK" or "NOK" or "DKK" => "Europe",
                "JPY" or "CNY" or "HKD" or "SGD" or "KRW" or "INR" => "Asia",
                "AUD" or "NZD" => "Oceania",
                "BRL" or "ARS" or "CLP" => "South America",
                "ZAR" => "Africa",
                _ => "Other"
            };
        }
        
        // Unable to determine
        return "Other";
    }
}
