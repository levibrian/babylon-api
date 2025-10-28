namespace Babylon.Alfred.Api.Shared.Data.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Navigation property - one user can have many transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
