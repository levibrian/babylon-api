using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

public class PortfolioTransactionDto
{
    public Guid Id { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime Date { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }
    public decimal Tax { get; set; }

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
}
