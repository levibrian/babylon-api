using System.ComponentModel.DataAnnotations.Schema;

namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid SecurityId { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime Date { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }
    public decimal Tax { get; set; } = 0;

    // Foreign key - required
    public Guid? UserId { get; set; }

    // Navigation properties
    public User? User { get; set; } = null!;
    public Security Security { get; set; } = null!;
    
    public decimal? RealizedPnL { get; set; }
    public decimal? RealizedPnLPct { get; set; }


    [NotMapped]
    public decimal Amount => SharesQuantity * SharePrice;

    [NotMapped]
    public decimal TotalAmount => TransactionType switch
    {
        TransactionType.Buy => Amount + Fees,
        TransactionType.Sell => Amount - Fees,
        TransactionType.Dividend => (SharesQuantity * SharePrice) - Tax,  // Gross - Tax = Net Income
        TransactionType.Split => 0,  // Stock splits don't involve money
        _ => Amount + Fees
    };
}

public enum TransactionType
{
    Buy,
    Sell,
    Dividend,
    Split
}
