using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models;

public class CreateTransactionRequest
{
    public string Ticker { get; set; }
    public string? CompanyName { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime? Date { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }
}
