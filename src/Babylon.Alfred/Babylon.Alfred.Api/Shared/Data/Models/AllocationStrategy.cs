namespace Babylon.Alfred.Api.Shared.Data.Models;

public class AllocationStrategy
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public decimal TargetPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
}

