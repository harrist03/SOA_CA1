using SOA_CA1.Models;

namespace SOA_CA1.Services;

public sealed class PortfolioService(
    IHoldingsRepository holdingsRepository,
    IMarketDataService marketDataService,
    ICurrencyService currencyService) : IPortfolioService
{
    public async Task<Portfolio> GetPortfolioAsync(string displayCurrency)
    {
        var normalizedCurrency = NormalizeCurrencyCode(displayCurrency);
        var portfolio = await holdingsRepository.LoadAsync();
        var fxRate = normalizedCurrency == "USD"
            ? 1m
            : await currencyService.GetRateAsync("USD", normalizedCurrency);

        foreach (var holding in portfolio.Holdings)
        {
            holding.FxRate = fxRate;
            holding.Quote = null;

            try
            {
                holding.Quote = await marketDataService.GetQuoteAsync(holding.Symbol);
            }
            catch (ApiException)
            {
                // Keep the holding visible when its live quote is unavailable.
            }
        }

        return portfolio;
    }

    public IEnumerable<Holding> Sort(IEnumerable<Holding> holdings, string sortBy, bool descending)
    {
        ArgumentNullException.ThrowIfNull(holdings);

        if (!Enum.TryParse<SortField>(sortBy, ignoreCase: true, out var sortField))
        {
            throw new ArgumentException("An invalid holding sort field was provided.", nameof(sortBy));
        }

        return sortField switch
        {
            SortField.Symbol => descending
                ? holdings.OrderByDescending(holding => holding.Symbol, StringComparer.OrdinalIgnoreCase)
                : holdings.OrderBy(holding => holding.Symbol, StringComparer.OrdinalIgnoreCase),
            SortField.MarketValue => descending
                ? holdings.OrderByDescending(holding => holding.MarketValue ?? decimal.MinValue)
                : holdings.OrderBy(holding => holding.MarketValue ?? decimal.MaxValue),
            SortField.GainLossPercent => descending
                ? holdings.OrderByDescending(holding => holding.GainLossPercent ?? decimal.MinValue)
                : holdings.OrderBy(holding => holding.GainLossPercent ?? decimal.MaxValue),
            SortField.Quantity => descending
                ? holdings.OrderByDescending(holding => holding.Quantity)
                : holdings.OrderBy(holding => holding.Quantity),
            SortField.BuyPrice => descending
                ? holdings.OrderByDescending(holding => holding.BuyPrice * holding.FxRate)
                : holdings.OrderBy(holding => holding.BuyPrice * holding.FxRate),
            SortField.CurrentPrice => descending
                ? holdings.OrderByDescending(holding => holding.Quote is null
                    ? decimal.MinValue
                    : holding.Quote.C * holding.FxRate)
                : holdings.OrderBy(holding => holding.Quote is null
                    ? decimal.MaxValue
                    : holding.Quote.C * holding.FxRate),
            _ => throw new ArgumentException("An invalid holding sort field was provided.", nameof(sortBy))
        };
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("A display currency is required.", nameof(currencyCode));
        }

        return currencyCode.Trim().ToUpperInvariant();
    }
}
