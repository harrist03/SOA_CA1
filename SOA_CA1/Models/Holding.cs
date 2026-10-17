using System.Text.Json.Serialization;

namespace SOA_CA1.Models;

public class Holding : IComparable<Holding>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal BuyPrice { get; set; }
    public DateOnly DateBought { get; set; }
    [JsonIgnore] public StockQuote? Quote { get; set; }
    [JsonIgnore] public decimal FxRate { get; set; } = 1m;
    [JsonIgnore] public decimal CostBasis => Quantity * BuyPrice * FxRate;
    [JsonIgnore] public decimal? MarketValue => Quote is null ? null : Quantity * Quote.C * FxRate;
    [JsonIgnore] public decimal? GainLoss => MarketValue - CostBasis;
    [JsonIgnore] public decimal? GainLossPercent =>
        CostBasis == 0m || GainLoss is null ? null : GainLoss / CostBasis * 100m;

    public int CompareTo(Holding? other)
    {
        if (other is null)
        {
            return 1;
        }

        return string.Compare(Symbol, other.Symbol, StringComparison.OrdinalIgnoreCase);
    }
}
