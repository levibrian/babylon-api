using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Models;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Infrastructure
{
    public class YahooMarketDataServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly YahooMarketDataService _sut;

        public YahooMarketDataServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            var logger = Mock.Of<ILogger<YahooMarketDataService>>();

            _sut = new YahooMarketDataService(httpClient, logger);
        }

        [Fact]
        public async Task GetQuotesAsync_ShouldReturnQuotes_WhenSuccessful()
        {
            // Arrange
            var symbol = "AAPL";
            var crumb = "test_crumb";
            var cookie = "test_cookie=value; Domain=.yahoo.com; Path=/";
            
            // Mock Crumb Cookie Request (fc.yahoo.com)
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.Host == "fc.yahoo.com"),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Headers = { { "Set-Cookie", cookie } }
                });

            // Mock Crumb Value Request (getcrumb)
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsolutePath.Contains("getcrumb") && req.Headers.Contains("Cookie")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(crumb)
                });

            // Mock Quote Request
            var yahooResponse = new YahooResponse
            {
                QuoteResponse = new QuoteResponse
                {
                    Result = new List<YahooQuoteResult>
                    {
                        new YahooQuoteResult { Symbol = symbol, RegularMarketPrice = 150.0m }
                    }
                }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(yahooResponse);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsolutePath.Contains("quote") && req.Headers.Contains("Cookie")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            // Act
            var result = await _sut.GetQuotesAsync(new[] { symbol });

            // Assert
            result.Should().ContainKey(symbol);
            result[symbol].RegularMarketPrice.Should().Be(150.0m);
        }

        [Fact]
        public async Task GetQuotesAsync_ShouldRetry_WhenUnauthorized()
        {
            // Arrange
            var symbol = "MSFT";
            var crumb = "test_crumb";
            var cookie = "test_cookie=value";

            // Setup successful cookie/crumb acquisition
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.Host == "fc.yahoo.com"),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Headers = { { "Set-Cookie", cookie } }
                });

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsolutePath.Contains("getcrumb")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(crumb)
                });

            // Setup Quote Request: First fail (401), then succeed
            var yahooResponse = new YahooResponse
            {
                QuoteResponse = new QuoteResponse
                {
                    Result = new List<YahooQuoteResult>
                    {
                        new YahooQuoteResult { Symbol = symbol, RegularMarketPrice = 300.0m }
                    }
                }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(yahooResponse);

            _httpMessageHandlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsolutePath.Contains("quote")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized }) // First attempt fails
                .ReturnsAsync(new HttpResponseMessage // Second attempt succeeds
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            // Act
            var result = await _sut.GetQuotesAsync(new[] { symbol });

            // Assert
            result.Should().ContainKey(symbol);
            result[symbol].RegularMarketPrice.Should().Be(300.0m);
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnResults_WhenSuccessful()
        {
            // Arrange
            var query = "Apple";
            var searchResponse = new YahooSearchResponse
            {
                Quotes = new List<YahooSearchResult>
                {
                    new YahooSearchResult { Symbol = "AAPL", ShortName = "Apple Inc.", Exchange = "NMS", QuoteType = "EQUITY" }
                }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(searchResponse);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri.AbsolutePath.Contains("search") && 
                        req.RequestUri.Query.Contains(query)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            // Act
            var result = await _sut.SearchAsync(query);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(x => x.Symbol == "AAPL");
            result.First().ShortName.Should().Be("Apple Inc.");
        }
    }
}
