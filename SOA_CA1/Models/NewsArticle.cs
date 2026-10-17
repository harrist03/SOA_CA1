using System.Text.Json.Serialization;

namespace SOA_CA1.Models;

public record NewsArticle(
    [property: JsonPropertyName("headline")] string Headline,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("datetime")] long Datetime);
