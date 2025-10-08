using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ITransactionService
{
    Task<IList<Transaction>> GetAllTransactionsAsync();
    Task<Transaction> CreateAsync(CreateTransactionRequest request);
}
