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
    public decimal? RealizedPnL { get; set; }
    public decimal? RealizedPnLPct { get; set; }

    public decimal TotalAmount
    {
        get
        {
            return TransactionType switch
            {
                TransactionType.Buy => (SharesQuantity * SharePrice) + Fees + Tax,
                TransactionType.Sell => (SharesQuantity * SharePrice) - Fees - Tax,
                TransactionType.Dividend => (SharesQuantity * SharePrice) - Tax,  // Gross - Tax = Net Income
                TransactionType.Split => 0,  // Stock splits don't involve money
                _ => (SharesQuantity * SharePrice) + Fees
            };
        }
        private set { } // Allow serialization
    }

    public decimal? ImpliedDividendRate => TransactionType == TransactionType.Dividend ? SharePrice : null;
}

