namespace Babylon.Alfred.Api.Shared.Data.Models;

public class AllocationStrategy
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal TargetPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}

