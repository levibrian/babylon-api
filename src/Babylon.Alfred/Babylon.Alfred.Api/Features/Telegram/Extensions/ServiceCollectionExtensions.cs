using Telegram.Bot;

namespace Babylon.Alfred.Api.Features.Telegram.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterTelegram(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ITelegramBotClient>(provider =>
        {
            // var config = provider.GetRequiredService<IConfiguration>();
            //
            // var token = config["Telegram:Token"]!;
            
            return new TelegramBotClient("7518624842:AAHYHHF4oac1UqRTh0Rc1HslWkhK5F5PfU4");
        });
    }
}