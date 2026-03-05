namespace Babylon.Alfred.Api.Shared.Data.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; } // Nullable for Google-only users
    public string Email { get; set; } = string.Empty;

    public decimal MonthlyInvestmentAmount { get; set; }
    public string? AuthProvider { get; set; } // "Local", "Google", or "Local,Google" for hybrid
    public bool HasLocalAuth => !string.IsNullOrEmpty(Password);
    public bool HasGoogleAuth => AuthProvider?.Contains("Google") ?? false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property - one user can have many transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public CashBalance? CashBalance { get; set; }
}
