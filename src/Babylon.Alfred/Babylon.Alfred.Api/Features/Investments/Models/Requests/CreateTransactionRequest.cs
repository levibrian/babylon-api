using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public record CreateTransactionRequest
{
    public required string Ticker { get; init; }
    public TransactionType TransactionType { get; init; }
    public DateOnly? Date { get; init; }
    public decimal SharesQuantity { get; init; }
    public decimal SharePrice { get; init; }
    public decimal Fees { get; init; }
    public decimal Tax { get; init; } = 0;
    public decimal? TotalAmount { get; init; } // For dividends: Net Amount (Gross - Tax)

    public Guid? UserId { get; init; }
}
