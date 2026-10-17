using System.Text.Json;
using SOA_CA1.Models;

namespace SOA_CA1.Services;

public class MarketDataService
{
    private const string ProviderName = "Finnhub";
    private readonly HttpClient httpClient;
    private readonly string apiKey;

    public MarketDataService(HttpClient httpClient, IConfiguration configuration)
    {
        this.httpClient = httpClient;
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
        catch (ApiException)
        {
            throw;
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
}
