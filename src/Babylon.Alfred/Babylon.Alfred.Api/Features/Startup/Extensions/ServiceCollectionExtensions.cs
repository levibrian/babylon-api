using Babylon.Alfred.Api.Features.Investments.Extensions;
using Babylon.Alfred.Api.Features.RecurringSchedules.Extensions;
using Babylon.Alfred.Api.Features.Telegram.Extensions;

namespace Babylon.Alfred.Api.Features.Startup.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterFeatures(this IServiceCollection serviceCollection)
    {
        serviceCollection.RegisterTelegram();
        serviceCollection.RegisterInvestmentServices();
        serviceCollection.RegisterRecurringScheduleServices();
        
        // Register Authentication
        serviceCollection.AddScoped<Features.Authentication.Services.IAuthService, Features.Authentication.Services.AuthService>();
        serviceCollection.AddScoped<Features.Authentication.Services.IAccountLinkingService, Features.Authentication.Services.AccountLinkingService>();
        serviceCollection.AddScoped<Features.Authentication.Services.IPasswordService, Features.Authentication.Services.PasswordService>();
        serviceCollection.AddSingleton<Features.Authentication.Utils.JwtTokenGenerator>();
    }
}
