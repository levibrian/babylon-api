namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Security
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<AllocationStrategy> AllocationStrategies { get; set; } = new List<AllocationStrategy>();
}

