using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> Add(Transaction transaction);
    public Task<IList<Transaction?>> AddBulk(IList<Transaction?> transactions);
    Task<IEnumerable<Transaction>> GetAll();
    Task<IEnumerable<Transaction>> GetOpenPositionsByUser(Guid userId);
    Task<IEnumerable<Transaction>> GetAllByUser(Guid userId);
    Task<Transaction?> GetById(Guid transactionId, Guid userId);
    Task<Transaction> Update(Transaction transaction);
    Task<Transaction> Delete(Guid transactionId, Guid userId);
}

