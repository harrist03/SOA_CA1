using System.Text.Json.Serialization;

namespace SOA_CA1.Models;

public class Portfolio
{
    public string Name { get; set; } = "My Portfolio";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public List<Holding> Holdings { get; set; } = new();
    [JsonIgnore] public decimal TotalCost => Holdings.Sum(holding => holding.CostBasis);
    [JsonIgnore] public decimal TotalValue => Holdings.Sum(holding => holding.MarketValue ?? 0m);
    [JsonIgnore] public decimal TotalGainLoss => TotalValue - TotalCost;
}
