using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class CreateTransactionRequest
{
    public required string Ticker { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateOnly? Date { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }

    public Guid? UserId { get; set; }
}
