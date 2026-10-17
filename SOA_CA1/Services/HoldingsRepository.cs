using System.Text.Json;
using SOA_CA1.Models;

namespace SOA_CA1.Services;

public sealed class HoldingsRepository : IHoldingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string filePath;

    public HoldingsRepository(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        filePath = Path.Combine(environment.ContentRootPath, "Data", "holdings.json");
    }

    public async Task<Portfolio> LoadAsync()
    {
        if (!File.Exists(filePath))
        {
            return new Portfolio();
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var portfolio = await JsonSerializer.DeserializeAsync<Portfolio>(stream, JsonOptions);
            return portfolio ?? throw new InvalidDataException("The holdings seed file does not contain a portfolio.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The holdings seed file contains invalid JSON.", exception);
        }
    }
}
