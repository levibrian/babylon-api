using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> Add(Transaction transaction);
    public Task<IList<Transaction?>> AddBulk(IList<Transaction?> transactions);
    Task<IEnumerable<Transaction>> GetAll();
    Task<IEnumerable<Transaction>> GetOpenPositionsByUser(Guid userId);
    Task<IEnumerable<Transaction>> GetAllByUser(Guid userId);
    Task<IList<Guid>> GetDistinctUserIdsWithUnbackfilledSellsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetDividendTransactionsByUser(Guid userId);
    Task<IEnumerable<Transaction>> GetBuyAndSellTransactionsByUserAndSecurity(Guid userId, Guid securityId);
    Task<Transaction?> GetById(Guid transactionId, Guid userId);
    Task<Transaction> Update(Transaction transaction);
    Task UpdateBulkAsync(IList<Transaction> transactions, CancellationToken cancellationToken = default);
    Task<Transaction> Delete(Guid transactionId, Guid userId);
}

