using System.ComponentModel.DataAnnotations.Schema;

namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public DateTime Date { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }

    [NotMapped]
    public decimal Amount => SharesQuantity * SharePrice;

    [NotMapped]
    public decimal TotalAmount => Amount + Fees;
}

public enum TransactionType
{
    Buy,
    Sell
}
