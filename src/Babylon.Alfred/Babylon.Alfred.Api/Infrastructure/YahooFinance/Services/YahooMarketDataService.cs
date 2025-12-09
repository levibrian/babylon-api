using System.Net;
using System.Text.Json;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Models;

namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Services
{
    /// <summary>
    /// Service that handles Yahoo Finance crumb acquisition and quote retrieval.
    /// </summary>
    public class YahooMarketDataService : IYahooMarketDataService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<YahooMarketDataService> logger;
        private string? crumb;
        private readonly CookieContainer cookieContainer = new();

        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
        private static readonly Uri CrumbBaseUri = new("https://fc.yahoo.com");
        private static readonly Uri CrumbUri = new("https://query1.finance.yahoo.com/v1/test/getcrumb");
        private static readonly Uri QuoteUri = new("https://query1.finance.yahoo.com/v7/finance/quote");

        public YahooMarketDataService(HttpClient httpClient, ILogger<YahooMarketDataService> logger)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Ensure the HttpClient uses a handler that shares the cookie container.
            if (this.httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
                this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        private async Task EnsureCrumbAsync()
        {
            if (!string.IsNullOrEmpty(crumb))
                return;

            // Step 1 – obtain cookie from fc.yahoo.com
            // We must manually handle cookies because we cannot modify the handler of the injected HttpClient.
            var request = new HttpRequestMessage(HttpMethod.Get, CrumbBaseUri);
            var cookieResponse = await httpClient.SendAsync(request);
            cookieResponse.EnsureSuccessStatusCode();

            if (cookieResponse.Headers.TryGetValues("Set-Cookie", out var cookieValues))
            {
                foreach (var cookie in cookieValues)
                {
                    cookieContainer.SetCookies(CrumbBaseUri, cookie);
                }
            }

            // Step 2 – obtain crumb using the stored cookie
            var crumbRequest = new HttpRequestMessage(HttpMethod.Get, CrumbUri);
            var cookieHeader = cookieContainer.GetCookieHeader(CrumbUri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                crumbRequest.Headers.Add("Cookie", cookieHeader);
            }

            var crumbResponse = await httpClient.SendAsync(crumbRequest);
            crumbResponse.EnsureSuccessStatusCode();
            crumb = await crumbResponse.Content.ReadAsStringAsync();
            logger.LogInformation("Acquired Yahoo crumb: {Crumb}", crumb);
        }

        public async Task<Dictionary<string, YahooQuoteResult>> GetQuotesAsync(IEnumerable<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(tickers);
            await EnsureCrumbAsync();

            var symbols = string.Join(",", tickers);
            var crumb = this.crumb ?? string.Empty;
            var requestUri = new UriBuilder(QuoteUri)
            {
                Query = $"symbols={Uri.EscapeDataString(symbols)}&crumb={Uri.EscapeDataString(crumb)}"
            }.Uri;

            var response = await SendRequestWithCookiesAsync(requestUri);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Crumb likely expired – reset and retry once.
                this.crumb = null;
                await EnsureCrumbAsync();

                // Rebuild URI with new crumb
                var newCrumb = this.crumb ?? string.Empty;
                requestUri = new UriBuilder(QuoteUri)
                {
                    Query = $"symbols={Uri.EscapeDataString(symbols)}&crumb={Uri.EscapeDataString(newCrumb)}"
                }.Uri;
                response = await SendRequestWithCookiesAsync(requestUri);
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var yahooResponse = JsonSerializer.Deserialize<YahooResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var dict = new Dictionary<string, YahooQuoteResult>(StringComparer.OrdinalIgnoreCase);
            if (yahooResponse?.QuoteResponse?.Result != null)
            {
                foreach (var result in yahooResponse.QuoteResponse.Result)
                {
                    dict[result.Symbol] = result;
                }
            }
            return dict;
        }

        public async Task<List<YahooSearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            // Search does not require a crumb in some endpoints, but let's use the standard search API.
            // URL: https://query1.finance.yahoo.com/v1/finance/search?q={query}
            var requestUri = new Uri($"https://query1.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}");

            // We can reuse the cookie/crumb logic if needed, but often search is public. 
            // However, to be consistent and avoid rate limits/blocks, let's use the authenticated client flow if we have cookies.
            // Just sending a normal GET request with the shared HttpClient.
            
            var response = await httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<YahooSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return searchResponse?.Quotes ?? [];
        }

        private async Task<HttpResponseMessage> SendRequestWithCookiesAsync(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var cookieHeader = cookieContainer.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }
            return await httpClient.SendAsync(request);
        }
    }

    public interface IYahooMarketDataService
    {
        Task<Dictionary<string, YahooQuoteResult>> GetQuotesAsync(IEnumerable<string> tickers);
        Task<List<YahooSearchResult>> SearchAsync(string query);
    }
}
