using System.Net;
using Babylon.Alfred.Worker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Babylon.Alfred.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureYahooClient(this IServiceCollection services)
    {
        services.AddHttpClient<YahooFinanceService>(client =>
        {
            // Configure browser-like headers to avoid Yahoo Finance bot detection
            // Note: Some headers like Connection are managed by HttpClient and shouldn't be set manually
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("Referer", "https://finance.yahoo.com/");
            client.DefaultRequestHeaders.Add("Origin", "https://finance.yahoo.com");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            UseCookies = true
        });

        return services;
    }
}

