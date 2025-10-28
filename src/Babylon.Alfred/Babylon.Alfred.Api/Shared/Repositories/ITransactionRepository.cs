using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> Add(Transaction transaction);
    public Task<IList<Transaction?>> AddBulk(IList<Transaction?> transactions);
    Task<IEnumerable<Transaction>> GetAll();
}

