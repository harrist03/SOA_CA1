using System.Net;
using System.Text;
using SOA_CA1.Services;

namespace SOA_CA1.Tests;

public class CurrencyServiceTests
{
    [Fact]
    public async Task GetRateAsync_ReturnsLatestRate()
    {
        var handler = new StubHttpMessageHandler(
            """[{"date":"2026-10-17","base":"USD","quote":"EUR","rate":0.89057}]""");
        var service = CreateService(handler);

        var rate = await service.GetRateAsync("usd", "eur");

        Assert.Equal(0.89057m, rate);
        Assert.Equal("v2/rates?base=USD&quotes=EUR", handler.RequestUri);
    }

    [Fact]
    public async Task GetRateAsync_WhenRateIsMissing_ThrowsApiException()
    {
        var service = CreateService(
            new StubHttpMessageHandler("[]"));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.GetRateAsync("USD", "EUR"));

        Assert.Equal("Frankfurter", exception.Provider);
        Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
    }

    [Fact]
    public async Task GetRateAsync_WhenJsonIsMalformed_ThrowsApiException()
    {
        var service = CreateService(new StubHttpMessageHandler("{ malformed json"));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => service.GetRateAsync("USD", "EUR"));

        Assert.Equal("Frankfurter", exception.Provider);
        Assert.Null(exception.StatusCode);
    }

    private static CurrencyService CreateService(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.frankfurter.dev/v2/")
        };

        return new CurrencyService(httpClient);
    }

    // Return controlled HTTP responses without calling Frankfurter.
    private sealed class StubHttpMessageHandler(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.PathAndQuery.TrimStart('/');

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
