using Babylon.Alfred.Api.Features.Profiling.Services;

namespace Babylon.Alfred.Api.Features.Profiling.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterProfilingServices(this IServiceCollection services)
    {
        services.AddScoped<IInvestorProfileService, InvestorProfileService>();
    }
}
