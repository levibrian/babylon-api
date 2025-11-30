using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string SecurityName { get; set; } = string.Empty;
    public SecurityType SecurityType { get; set; }
    public DateTime Date { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }
    public decimal Tax { get; set; }
    public TransactionType TransactionType { get; set; }

    public decimal TotalAmount
    {
        get
        {
            if (TransactionType == TransactionType.Dividend)
                return (SharesQuantity * SharePrice) - Tax; // Gross - Tax = Net Income

            // Standard Buy/Sell
            return (SharesQuantity * SharePrice) + Fees; // Principal + Cost = Total Spent
        }
        private set { } // Allow serialization
    }

    public decimal? ImpliedDividendRate => TransactionType == TransactionType.Dividend ? SharePrice : null;
}

