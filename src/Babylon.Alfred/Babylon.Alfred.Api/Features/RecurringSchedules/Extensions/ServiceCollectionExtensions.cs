using Babylon.Alfred.Api.Features.RecurringSchedules.Services;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.RecurringSchedules.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterRecurringScheduleServices(this IServiceCollection services)
    {
        // Repository
        services.AddScoped<IRecurringScheduleRepository, RecurringScheduleRepository>();

        // Service
        services.AddScoped<IRecurringScheduleService, RecurringScheduleService>();
    }
}

