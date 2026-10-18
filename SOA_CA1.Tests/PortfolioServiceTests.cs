using SOA_CA1.Models;
using SOA_CA1.Services;

namespace SOA_CA1.Tests;

public class PortfolioServiceTests
{
    [Fact]
    public async Task GetPortfolioAsync_AppliesCurrencyRateAndLoadsQuotes()
    {
        var holding = CreateHolding("AAPL", 10m, 100m);
        var portfolio = new Portfolio { Holdings = [holding] };
        var service = CreateService(
            portfolio,
            new Dictionary<string, StockQuote> { ["AAPL"] = CreateQuote(120m) },
            0.9m);

        var result = await service.GetPortfolioAsync("eur");

        Assert.Equal(0.9m, holding.FxRate);
        Assert.Equal(120m, holding.Quote!.C);
        Assert.Equal(900m, holding.CostBasis);
        Assert.Equal(1080m, holding.MarketValue);
        Assert.Equal(180m, holding.GainLoss);
    }

    [Fact]
    public async Task GetPortfolioAsync_ForUsdUsesRateOfOneWithoutCallingCurrencyService()
    {
        var holding = CreateHolding("AAPL", 1m, 100m);
        var currencyService = new FakeCurrencyService(0.9m);
        var service = CreateService(
            new Portfolio { Holdings = [holding] },
            new Dictionary<string, StockQuote> { ["AAPL"] = CreateQuote(120m) },
            currencyService: currencyService);

        await service.GetPortfolioAsync("USD");

        Assert.Equal(1m, holding.FxRate);
        Assert.Equal(0, currencyService.CallCount);
    }

    [Fact]
    public async Task GetPortfolioAsync_WhenOneQuoteFails_LeavesOnlyThatQuoteUnavailable()
    {
        var available = CreateHolding("AAPL", 1m, 100m);
        var unavailable = CreateHolding("INVALID", 1m, 100m);
        var service = CreateService(
            new Portfolio { Holdings = [available, unavailable] },
            new Dictionary<string, StockQuote> { ["AAPL"] = CreateQuote(120m) },
            currencyRate: 1m);

        var result = await service.GetPortfolioAsync("USD");

        Assert.Same(result.Holdings[0], available);
        Assert.NotNull(available.Quote);
        Assert.Null(unavailable.Quote);
    }

    [Fact]
    public void Sort_OrdersByRequestedFieldAndDirection()
    {
        var apple = CreateHolding("AAPL", 1m, 100m);
        apple.Quote = CreateQuote(120m);
        var microsoft = CreateHolding("MSFT", 1m, 100m);
        microsoft.Quote = CreateQuote(150m);

        var service = CreateService(new Portfolio(), new Dictionary<string, StockQuote>());

        var sorted = service.Sort([apple, microsoft], "MarketValue", descending: true).ToArray();

        Assert.Equal(["MSFT", "AAPL"], sorted.Select(holding => holding.Symbol));
    }

    [Theory]
    [InlineData("Quantity", false, "AAPL", "MSFT")]
    [InlineData("Quantity", true, "MSFT", "AAPL")]
    [InlineData("BuyPrice", false, "AAPL", "MSFT")]
    [InlineData("BuyPrice", true, "MSFT", "AAPL")]
    [InlineData("CurrentPrice", false, "AAPL", "MSFT")]
    [InlineData("CurrentPrice", true, "MSFT", "AAPL")]
    public void Sort_OrdersByAdditionalHoldingFields(
        string sortField,
        bool descending,
        string firstSymbol,
        string secondSymbol)
    {
        var apple = CreateHolding("AAPL", 1m, 100m);
        apple.Quote = CreateQuote(120m);
        var microsoft = CreateHolding("MSFT", 2m, 200m);
        microsoft.Quote = CreateQuote(150m);

        var service = CreateService(new Portfolio(), new Dictionary<string, StockQuote>());

        var sorted = service.Sort([apple, microsoft], sortField, descending).ToArray();

        Assert.Equal([firstSymbol, secondSymbol], sorted.Select(holding => holding.Symbol));
    }

    [Fact]
    public void Sort_WhenFieldIsInvalid_ThrowsArgumentException()
    {
        var service = CreateService(new Portfolio(), new Dictionary<string, StockQuote>());

        Assert.Throws<ArgumentException>(() => service.Sort([], "Unknown", false).ToArray());
    }

    private static PortfolioService CreateService(
        Portfolio portfolio,
        Dictionary<string, StockQuote> quotes,
        decimal currencyRate = 1m,
        FakeCurrencyService? currencyService = null)
    {
        return new PortfolioService(
            new FakeHoldingsRepository(portfolio),
            new FakeMarketDataService(quotes),
            currencyService ?? new FakeCurrencyService(currencyRate));
    }

    private static Holding CreateHolding(string symbol, decimal quantity, decimal buyPrice)
    {
        return new Holding
        {
            Symbol = symbol,
            Quantity = quantity,
            BuyPrice = buyPrice
        };
    }

    private static StockQuote CreateQuote(decimal currentPrice)
    {
        return new StockQuote(currentPrice, null, null, 0m, 0m, 0m, 0m, null);
    }

    private sealed class FakeHoldingsRepository(Portfolio portfolio) : IHoldingsRepository
    {
        public Task<Portfolio> LoadAsync() => Task.FromResult(portfolio);
    }

    private sealed class FakeMarketDataService(Dictionary<string, StockQuote> quotes) : IMarketDataService
    {
        public Task<IReadOnlyList<SearchResult>> SearchAsync(string query) =>
            throw new NotSupportedException();

        public Task<StockQuote> GetQuoteAsync(string symbol)
        {
            if (!quotes.TryGetValue(symbol, out var quote))
            {
                throw new ApiException("Finnhub", null, "Quote unavailable.");
            }

            return Task.FromResult(quote);
        }

        public Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string symbol, int days = 7) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCurrencyService(decimal rate) : ICurrencyService
    {
        public int CallCount { get; private set; }

        public Task<decimal> GetRateAsync(string from, string to)
        {
            CallCount++;
            return Task.FromResult(rate);
        }

    }
}
