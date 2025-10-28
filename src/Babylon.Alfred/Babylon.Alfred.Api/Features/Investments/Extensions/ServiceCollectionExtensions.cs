using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterInvestmentServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICompanyService, CompanyService>();
    }
}

