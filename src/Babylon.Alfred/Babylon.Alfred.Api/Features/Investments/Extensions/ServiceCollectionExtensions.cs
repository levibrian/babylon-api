using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterInvestmentServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ISecurityRepository, SecurityRepository>();
        services.AddScoped<IMarketPriceRepository, MarketPriceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPortfolioSnapshotRepository, PortfolioSnapshotRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICashBalanceRepository, CashBalanceRepository>();

        // Services
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IMarketPriceService, MarketPriceService>();
        services.AddScoped<IPortfolioHistoryService, PortfolioHistoryService>();
        services.AddScoped<ICashBalanceService, CashBalanceService>();

        // External Services
        services.AddHttpClient<IYahooMarketDataService, YahooMarketDataService>();
        services.AddHttpClient<IHistoricalPriceService, HistoricalPriceService>();
    }
}
