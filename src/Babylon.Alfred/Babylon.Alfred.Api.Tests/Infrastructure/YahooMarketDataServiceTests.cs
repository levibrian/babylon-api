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
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<YahooMarketDataService>> _loggerMock;
        private readonly YahooMarketDataService _sut;

        public YahooMarketDataServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _loggerMock = new Mock<ILogger<YahooMarketDataService>>();
            _sut = new YahooMarketDataService(_httpClient, _loggerMock.Object);
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

        [Fact]
        public async Task SearchAsync_ShouldReturnEmptyList_WhenApiFails()
        {
            // Arrange
            var query = "Apple";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            // Act
            var result = await _sut.SearchAsync(query);

            // Assert
            result.Should().BeEmpty();
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error searching Yahoo Finance")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
