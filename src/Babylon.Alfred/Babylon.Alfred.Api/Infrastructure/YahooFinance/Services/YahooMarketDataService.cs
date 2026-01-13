using System.Text.Json;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Models;

namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Services
{
    /// <summary>
    /// Service that handles Yahoo Finance search retrieval.
    /// </summary>
    public class YahooMarketDataService : IYahooMarketDataService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<YahooMarketDataService> logger;

        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

        public YahooMarketDataService(HttpClient httpClient, ILogger<YahooMarketDataService> logger)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (this.httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
                this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        public async Task<List<YahooSearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            // URL: https://query1.finance.yahoo.com/v1/finance/search?q={query}
            var requestUri = new Uri($"https://query1.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}");

            try
            {
                var response = await httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<YahooSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return searchResponse?.Quotes ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching Yahoo Finance for {Query}", query);
                return [];
            }
        }
    }

    public interface IYahooMarketDataService
    {
        Task<List<YahooSearchResult>> SearchAsync(string query);
    }
}
