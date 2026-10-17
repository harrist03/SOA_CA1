using System.Net;
using System.Text;
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

        return new MarketDataService(httpClient, configuration);
    }

    // Return controlled HTTP responses without calling Finnhub.
    private sealed class StubHttpMessageHandler(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            RequestUri = request.RequestUri?.PathAndQuery.TrimStart('/');

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
