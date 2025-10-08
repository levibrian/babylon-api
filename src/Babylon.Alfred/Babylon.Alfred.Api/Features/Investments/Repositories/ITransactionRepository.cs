using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> AddAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetAllAsync();
}

