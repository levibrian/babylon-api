namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Security
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? Isin { get; set; }
    public string SecurityName { get; set; } = string.Empty;
    public SecurityType SecurityType { get; set; } = SecurityType.Stock;
    public string? Currency { get; set; }
    public string? Exchange { get; set; }
    
    // Fundamental metadata for analytics
    public string? Sector { get; set; }         // e.g., "Technology", "Healthcare"
    public string? Industry { get; set; }       // e.g., "Consumer Electronics", "Pharmaceuticals"
    public string? Geography { get; set; }      // e.g., "North America", "Europe", "Asia"
    public decimal? MarketCap { get; set; }     // Market capitalization in USD
    
    public DateTime? LastUpdated { get; set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<AllocationStrategy> AllocationStrategies { get; set; } = new List<AllocationStrategy>();
}

