using SOA_CA1.Models;

namespace SOA_CA1.Services;

public interface IMarketDataService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query);
    Task<StockQuote> GetQuoteAsync(string symbol);
    Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string symbol, int days = 7);
}
