namespace SOA_CA1.Services;

public interface ICurrencyService
{
    Task<decimal> GetRateAsync(string from, string to);
    Task<IReadOnlyList<string>> GetCurrenciesAsync();
}
