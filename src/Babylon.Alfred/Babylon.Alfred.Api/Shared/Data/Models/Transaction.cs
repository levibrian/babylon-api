namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Transaction
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime Date { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }

    public decimal Amount => SharesQuantity * SharePrice;
    public decimal TotalAmount => Amount + Fees;
}

public enum TransactionType
{
    Buy,
    Sell
}
