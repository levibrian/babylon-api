namespace Babylon.Alfred.Api.Shared.Data.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; } // Nullable for Google-only users
    public string Email { get; set; } = string.Empty;

    public decimal MonthlyInvestmentAmount { get; set; }
    public string? AuthProvider { get; set; } // "Local" or "Google"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property - one user can have many transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
