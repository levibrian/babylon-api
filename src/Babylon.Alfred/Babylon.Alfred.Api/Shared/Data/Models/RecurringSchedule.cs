namespace Babylon.Alfred.Api.Shared.Data.Models;

public class RecurringSchedule
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SecurityId { get; set; }
    public string? Platform { get; set; }
    public decimal? TargetAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Security Security { get; set; } = null!;
}

