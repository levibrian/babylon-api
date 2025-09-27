using Babylon.Alfred.Api.Features.Investments.Services;

namespace Babylon.Alfred.Api.Features.Investments.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterInvestments(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IInvestmentsService, InvestmentsService>();
    }
}
