using System.Text.Json.Serialization;

namespace SOA_CA1.Models;

public record SearchResult(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("type")] string Type);
