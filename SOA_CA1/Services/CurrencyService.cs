using System.Text.Json;

namespace SOA_CA1.Services;

public class CurrencyService : ICurrencyService
{
    private const string ProviderName = "Frankfurter";
    private readonly HttpClient httpClient;

    public CurrencyService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<decimal> GetRateAsync(string from, string to)
    {
        var normalizedFrom = NormalizeCurrencyCode(from);
        var normalizedTo = NormalizeCurrencyCode(to);
        var requestUri = $"rates?base={normalizedFrom}&quotes={normalizedTo}";

        try
        {
            using var response = await httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Frankfurter could not retrieve the exchange rate.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(responseStream);
            var rateEntry = document.RootElement.EnumerateArray()
                .FirstOrDefault(entry =>
                    entry.TryGetProperty("base", out var baseElement) &&
                    entry.TryGetProperty("quote", out var quoteElement) &&
                    string.Equals(baseElement.GetString(), normalizedFrom, StringComparison.Ordinal) &&
                    string.Equals(quoteElement.GetString(), normalizedTo, StringComparison.Ordinal));

            if (rateEntry.ValueKind == JsonValueKind.Undefined ||
                !rateEntry.TryGetProperty("rate", out var rateElement) ||
                !rateElement.TryGetDecimal(out var rate))
            {
                throw new ApiException(ProviderName, response.StatusCode,
                    "Frankfurter returned an invalid exchange-rate response.");
            }

            return rate;
        }
        catch (JsonException)
        {
            throw new ApiException(ProviderName, null,
                "Frankfurter returned malformed exchange-rate data.");
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException(ProviderName, exception.StatusCode,
                "Frankfurter could not be reached.");
        }
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ApiException(ProviderName, null, "A currency code is required.");
        }

        return currencyCode.Trim().ToUpperInvariant();
    }
}
