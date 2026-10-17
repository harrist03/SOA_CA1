using System.Text.Json.Serialization;

namespace SOA_CA1.Models;

public record StockQuote(
    [property: JsonPropertyName("c")] decimal C, // current price
    [property: JsonPropertyName("d")] decimal? D, // price change
    [property: JsonPropertyName("dp")] decimal? Dp, // percentage change
    [property: JsonPropertyName("h")] decimal H, // daily high
    [property: JsonPropertyName("l")] decimal L, // daily low
    [property: JsonPropertyName("o")] decimal O, // opening price
    [property: JsonPropertyName("pc")] decimal Pc, // prev close
    [property: JsonPropertyName("t")] long? T); // unix timestamp
