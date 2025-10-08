using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ITransactionService
{
    IList<Transaction> GetAllTransactions();
}
