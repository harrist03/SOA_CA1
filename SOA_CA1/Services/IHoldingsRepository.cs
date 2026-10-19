using SOA_CA1.Models;

namespace SOA_CA1.Services;

public interface IHoldingsRepository
{
    Task<Portfolio> LoadAsync();
    Task SaveAsync(Portfolio portfolio);
}
