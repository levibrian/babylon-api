using Babylon.Alfred.Api.Features.Import.Services;

namespace Babylon.Alfred.Api.Features.Import.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterImportServices(this IServiceCollection services)
    {
        services.AddScoped<ICsvImportService, CsvImportService>();
    }
}
