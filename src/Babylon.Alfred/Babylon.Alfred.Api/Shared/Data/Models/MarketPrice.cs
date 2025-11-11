namespace Babylon.Alfred.Api.Shared.Data.Models;

public class MarketPrice
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime LastUpdated { get; set; }
}

