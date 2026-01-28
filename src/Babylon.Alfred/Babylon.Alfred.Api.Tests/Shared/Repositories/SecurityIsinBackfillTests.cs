using System.Net;
using System.Text;
using System.Text.Json;
using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

/// <summary>
/// Test suite for backfilling ISIN numbers on existing securities.
/// Uses OpenFIGI API (Bloomberg's free ISIN lookup service).
/// Note: This test is designed to be run manually for data backfill.
/// </summary>
public class SecurityIsinBackfillTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly SecurityRepository repository;
    private readonly HttpClient httpClient;

    public SecurityIsinBackfillTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        var logger = Mock.Of<ILogger<SecurityRepository>>();
        repository = new SecurityRepository(context, logger);
        httpClient = new HttpClient();
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
        httpClient.Dispose();
    }

    /// <summary>
    /// Looks up ISIN from OpenFIGI API using ticker and exchange.
    /// OpenFIGI API: https://www.openfigi.com/api
    /// Rate limit: 25 requests per 5 minutes (without API key)
    /// </summary>
    private async Task<string?> LookupIsinAsync(string ticker, string? exchange = null)
    {
        try
        {
            var mapping = new
            {
                idType = "TICKER",
                idValue = ticker,
                exchCode = exchange
            };

            var requestBody = JsonSerializer.Serialize(new[] { mapping });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.openfigi.com/v3/mapping", content);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Rate limited - wait 12 seconds (5 min / 25 requests)
                await Task.Delay(12000);
                response = await httpClient.PostAsync("https://api.openfigi.com/v3/mapping", content);
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);

            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var firstMapping = root[0];
                if (firstMapping.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    var firstData = data[0];
                    if (firstData.TryGetProperty("shareClassFIGI", out var figi))
                    {
                        // OpenFIGI might return multiple matches, get ISIN from first match
                        if (firstData.TryGetProperty("isin", out var isinElement))
                        {
                            return isinElement.GetString();
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Manual test to backfill ISINs for specific securities.
    /// Add tickers to the list and run this test to populate ISINs.
    /// </summary>
    [Fact(Skip = "Manual execution only - uncomment to run backfill")]
    public async Task BackfillIsin_ForSpecificSecurities()
    {
        // Add securities to backfill
        var tickersToBackfill = new[]
        {
            ("AAPL", "US"),   // Apple Inc.
            ("GOOGL", "US"),  // Alphabet Class A
            ("GOOG", "US"),   // Alphabet Class C
            ("MSFT", "US"),   // Microsoft
            ("TSLA", "US"),   // Tesla
            ("AMZN", "US"),   // Amazon
            ("NVDA", "US"),   // NVIDIA
            ("META", "US"),   // Meta
        };

        foreach (var (ticker, exchange) in tickersToBackfill)
        {
            // Create test security
            var security = new Security
            {
                Ticker = ticker,
                SecurityName = $"{ticker} Inc.",
                SecurityType = SecurityType.Stock,
                Exchange = exchange,
                Isin = null
            };

            await repository.AddOrUpdateAsync(security);
        }

        // Backfill ISINs
        var securities = await context.Securities.Where(s => s.Isin == null).ToListAsync();
        var updatedCount = 0;

        foreach (var security in securities)
        {
            var isin = await LookupIsinAsync(security.Ticker, security.Exchange);

            if (!string.IsNullOrWhiteSpace(isin))
            {
                security.Isin = isin;
                context.Securities.Update(security);
                updatedCount++;

                Console.WriteLine($"Updated {security.Ticker}: ISIN = {isin}");
            }
            else
            {
                Console.WriteLine($"ISIN not found for {security.Ticker}");
            }

            // Respect rate limits
            await Task.Delay(250);
        }

        await context.SaveChangesAsync();

        Console.WriteLine($"Backfill complete: {updatedCount} securities updated");
        Assert.True(updatedCount > 0, "Should have updated at least one security");
    }

    /// <summary>
    /// Test to verify ISIN lookup works correctly with OpenFIGI API.
    /// </summary>
    [Fact(Skip = "External API call - enable manually for testing")]
    public async Task LookupIsin_WithValidTicker_ShouldReturnIsin()
    {
        // Act
        var isin = await LookupIsinAsync("AAPL", "US");

        // Assert
        Assert.NotNull(isin);
        Assert.Equal("US0378331005", isin); // Apple's ISIN
        Assert.Equal(12, isin.Length);
    }

    /// <summary>
    /// Helper test to verify existing securities can be queried for backfill.
    /// </summary>
    [Fact]
    public async Task GetSecuritiesWithoutIsin_ShouldReturnSecurities()
    {
        // Arrange
        var securities = new[]
        {
            new Security { Ticker = "AAPL", SecurityName = "Apple Inc.", SecurityType = SecurityType.Stock, Isin = "US0378331005" },
            new Security { Ticker = "MSFT", SecurityName = "Microsoft", SecurityType = SecurityType.Stock, Isin = null },
            new Security { Ticker = "GOOGL", SecurityName = "Alphabet", SecurityType = SecurityType.Stock, Isin = null }
        };

        await context.Securities.AddRangeAsync(securities);
        await context.SaveChangesAsync();

        // Act
        var securitiesWithoutIsin = await context.Securities
            .Where(s => s.Isin == null)
            .ToListAsync();

        // Assert
        Assert.Equal(2, securitiesWithoutIsin.Count);
        Assert.All(securitiesWithoutIsin, s => Assert.Null(s.Isin));
    }
}
