using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SOA_CA1.Models;

namespace SOA_CA1.Services;

public class MarketDataService : IMarketDataService
{
    private const string ProviderName = "Finnhub";
    private static readonly TimeSpan QuoteCacheDuration = TimeSpan.FromMinutes(5);
    private readonly HttpClient httpClient;
    private readonly IMemoryCache memoryCache;
    private readonly string apiKey;

    public MarketDataService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache memoryCache)
    {
        this.httpClient = httpClient;
        this.memoryCache = memoryCache;
        apiKey = configuration["Finnhub:ApiKey"]
            ?? throw new InvalidOperationException("Finnhub:ApiKey is not configured.");
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResult>();
        }

        var requestUri = $"search?q={Uri.EscapeDataString(query.Trim())}&token={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var response = await httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub could not complete the stock search.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(responseStream);
            if (!document.RootElement.TryGetProperty("result", out var resultElement))
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub returned an invalid stock search response.");
            }

            var results = resultElement.Deserialize<List<SearchResult>>();
            if (results is null)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub returned an invalid stock search response.");
            }

            return results
                .Where(result => string.Equals(result.Type, "Common Stock", StringComparison.OrdinalIgnoreCase))
                .Where(result => !result.Symbol.Contains('.', StringComparison.Ordinal))
                .ToArray();
        }
        catch (JsonException)
        {
            throw new ApiException(ProviderName, null,
                "Finnhub returned malformed stock search data.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(ProviderName, null,
                "Finnhub could not be reached.");
        }
    }

    public async Task<StockQuote> GetQuoteAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ApiException(ProviderName, null, "A stock symbol is required.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var cacheKey = $"finnhub:quote:{normalizedSymbol}";
        // If the quote is fetched again within 5 minutes, return the cached result
        if (memoryCache.TryGetValue(cacheKey, out StockQuote? cachedQuote) && cachedQuote is not null)
        {
            return cachedQuote;
        }

        var requestUri =
            $"quote?symbol={Uri.EscapeDataString(normalizedSymbol)}&token={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var response = await httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub could not retrieve the stock quote.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var quote = await JsonSerializer.DeserializeAsync<StockQuote>(responseStream);
            if (quote is null)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub returned an invalid stock quote.");
            }

            if (quote.C == 0m)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub could not find a quote for that stock symbol.");
            }

            memoryCache.Set(cacheKey, quote, QuoteCacheDuration);
            return quote;
        }
        catch (JsonException)
        {
            throw new ApiException(ProviderName, null,
                "Finnhub returned malformed stock quote data.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(ProviderName, null,
                "Finnhub could not be reached.");
        }
    }

    public async Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string symbol, int days = 7)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ApiException(ProviderName, null, "A stock symbol is required.");
        }

        if (days <= 0)
        {
            throw new ApiException(ProviderName, null, "The news period must be greater than zero days.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        // Get news articles from the x days ago
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-days);
        var requestUri =
            $"company-news?symbol={Uri.EscapeDataString(normalizedSymbol)}" +
            $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" +
            $"&token={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var response = await httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub could not retrieve company news.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var articles = await JsonSerializer.DeserializeAsync<List<NewsArticle>>(responseStream);
            if (articles is null)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Finnhub returned an invalid company news response.");
            }

            return articles;
        }
        catch (JsonException)
        {
            throw new ApiException(ProviderName, null,
                "Finnhub returned malformed company news data.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(ProviderName, null,
                "Finnhub could not be reached.");
        }
    }
}
