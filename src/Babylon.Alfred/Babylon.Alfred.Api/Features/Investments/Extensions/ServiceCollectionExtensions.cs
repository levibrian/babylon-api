using Babylon.Alfred.Api.Features.Investments.Repositories;
using Babylon.Alfred.Api.Features.Investments.Services;

namespace Babylon.Alfred.Api.Features.Investments.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterInvestmentServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionService, TransactionService>();
    }
}

