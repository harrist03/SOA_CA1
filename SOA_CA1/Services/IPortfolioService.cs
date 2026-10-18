using SOA_CA1.Models;

namespace SOA_CA1.Services;

public interface IPortfolioService
{
    Task<Portfolio> GetPortfolioAsync(string displayCurrency);
    IEnumerable<Holding> Sort(IEnumerable<Holding> holdings, string sortBy, bool descending);
}
