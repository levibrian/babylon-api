namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IMarketPriceService
{
    Task<decimal?> GetCurrentPriceAsync(string ticker);
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> tickers);
}

