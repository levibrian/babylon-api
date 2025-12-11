using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Babylon.Alfred.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureYahooClient(this IServiceCollection services)
    {
        // Register HttpClient without custom configuration
        // Headers are set in the YahooFinanceService constructor (same pattern as YahooMarketDataService)
        services.AddHttpClient<YahooFinanceService>();

        return services;
    }
}

