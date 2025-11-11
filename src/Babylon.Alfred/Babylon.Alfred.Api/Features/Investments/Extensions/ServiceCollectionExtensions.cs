using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterInvestmentServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IAllocationStrategyRepository, AllocationStrategyRepository>();
        services.AddScoped<IMarketPriceRepository, MarketPriceRepository>();
        
        // Services
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IMarketPriceService, MarketPriceService>();
        services.AddScoped<IAllocationStrategyService, AllocationStrategyService>();
        services.AddScoped<IPortfolioInsightsService, PortfolioInsightsService>();
    }
}

