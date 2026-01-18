namespace Babylon.Alfred.Api.Shared.Data.Models;

public class CashBalance
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public CashUpdateSource LastUpdatedSource { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
