using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SOA_CA1.Services;

namespace SOA_CA1.Tests;

public class MarketDataServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsOnlyCommonStocksWithoutPeriods()
    {
        const string responseJson = """
            {
              "result": [
                {
                  "symbol": "AAPL",
                  "description": "Apple Inc",
                  "type": "Common Stock"
                },
                {
                  "symbol": "AAPL.W",
                  "description": "Apple Warrant",
                  "type": "Common Stock"
                },
                {
                  "symbol": "AAPL ETF",
                  "description": "Apple-related ETF",
                  "type": "ETF"
                }
              ]
            }
            """;

        var handler = new StubHttpMessageHandler(responseJson);
        var service = CreateService(handler);

        var results = await service.SearchAsync("Apple");

        Assert.Equal(["AAPL"], results.Select(result => result.Symbol));
        Assert.Equal("api/v1/search?q=Apple&token=test-key", handler.RequestUri);
    }

    [Fact]
    public async Task SearchAsync_WithBlankQuery_ReturnsEmptyListWithoutCallingApi()
    {
        var handler = new StubHttpMessageHandler("{}");
        var service = CreateService(handler);

        var results = await service.SearchAsync(" ");

        Assert.Empty(results);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task SearchAsync_WhenRateLimited_ThrowsApiException()
    {
        var handler = new StubHttpMessageHandler("{}", HttpStatusCode.TooManyRequests);
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.SearchAsync("Apple"));

        Assert.Equal("Finnhub", exception.Provider);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_WhenJsonIsMalformed_ThrowsApiException()
    {
        var handler = new StubHttpMessageHandler("{ malformed json");
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.SearchAsync("Apple"));

        Assert.Equal("Finnhub", exception.Provider);
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_WhenResultPropertyIsMissing_ThrowsApiException()
    {
        var handler = new StubHttpMessageHandler("""{"count": 0}""");
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.SearchAsync("Apple"));

        Assert.Equal("Finnhub", exception.Provider);
        Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
    }

    [Fact]
    public async Task GetQuoteAsync_ReturnsQuoteAndCachesIt()
    {
        const string responseJson = """
            {
              "c": 343.46,
              "d": 1.46,
              "dp": 0.4269,
              "h": 347.82,
              "l": 340.46,
              "o": 346.085,
              "pc": 342,
              "t": 1792180800
            }
            """;

        var handler = new StubHttpMessageHandler(responseJson);
        var service = CreateService(handler);

        var firstQuote = await service.GetQuoteAsync("aapl");
        var secondQuote = await service.GetQuoteAsync("AAPL");

        Assert.Equal(343.46m, firstQuote.C);
        Assert.Same(firstQuote, secondQuote);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("api/v1/quote?symbol=AAPL&token=test-key", handler.RequestUri);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenQuoteHasZeroCurrentPrice_ThrowsApiException()
    {
        const string responseJson = """
            {
              "c": 0,
              "d": null,
              "dp": null,
              "h": 0,
              "l": 0,
              "o": 0,
              "pc": 0,
              "t": 0
            }
            """;

        var service = CreateService(new StubHttpMessageHandler(responseJson));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.GetQuoteAsync("INVALID"));

        Assert.Equal("Finnhub", exception.Provider);
        Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenJsonIsMalformed_ThrowsApiException()
    {
        var service = CreateService(new StubHttpMessageHandler("{ malformed json"));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.GetQuoteAsync("AAPL"));

        Assert.Equal("Finnhub", exception.Provider);
        Assert.Null(exception.StatusCode);
    }

    private static MarketDataService CreateService(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://finnhub.io/api/v1/")
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Finnhub:ApiKey"] = "test-key"
            })
            .Build();

        return new MarketDataService(httpClient, configuration, new MemoryCache(new MemoryCacheOptions()));
    }

    // Return controlled HTTP responses without calling Finnhub.
    private sealed class StubHttpMessageHandler(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public bool WasCalled { get; private set; }
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            CallCount++;
            RequestUri = request.RequestUri?.PathAndQuery.TrimStart('/');

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
