using Babylon.Alfred.Api.Features.Investments.Analyzers;
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
        services.AddScoped<IAllocationStrategyRepository, AllocationStrategyRepository>();
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
        services.AddScoped<IAllocationStrategyService, AllocationStrategyService>();
        services.AddScoped<IPortfolioAnalyticsService, PortfolioAnalyticsService>();
        services.AddScoped<IRebalancingService, RebalancingService>();
        services.AddScoped<IPortfolioHistoryService, PortfolioHistoryService>();
        services.AddScoped<ICashBalanceService, CashBalanceService>();

        // Portfolio Analyzers
        services.AddScoped<IPortfolioAnalyzer, RiskAnalyzer>();
        services.AddScoped<IPortfolioAnalyzer, IncomeAnalyzer>();
        services.AddScoped<IPortfolioAnalyzer, EfficiencyAnalyzer>();
        services.AddScoped<IPortfolioAnalyzer, TrendAnalyzer>();

        // Insights Service (depends on analyzers)
        services.AddScoped<IPortfolioInsightsService, PortfolioInsightsService>();

        // External Services
        services.AddHttpClient<IYahooMarketDataService, YahooMarketDataService>();
        services.AddHttpClient<IHistoricalPriceService, HistoricalPriceService>();
    }
}

