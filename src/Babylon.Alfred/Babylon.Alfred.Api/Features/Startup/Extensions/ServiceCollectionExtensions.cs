using Babylon.Alfred.Api.Features.Telegram.Extensions;

namespace Babylon.Alfred.Api.Features.Startup.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterFeatures(this IServiceCollection serviceCollection)
    {
        serviceCollection.RegisterTelegram();
    }
}
