namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Security
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string SecurityName { get; set; } = string.Empty;
    public SecurityType SecurityType { get; set; } = SecurityType.Stock;
    public string? Currency { get; set; }
    public string? Exchange { get; set; }
    public DateTime? LastUpdated { get; set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<AllocationStrategy> AllocationStrategies { get; set; } = new List<AllocationStrategy>();
}

