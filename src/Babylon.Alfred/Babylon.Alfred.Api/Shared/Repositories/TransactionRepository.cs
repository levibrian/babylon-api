using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class TransactionRepository(BabylonDbContext context) : ITransactionRepository
{
    public async Task<Transaction> Add(Transaction transaction)
    {
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IList<Transaction?>> AddBulk(IList<Transaction?> transactions)
    {
        // This method generates a single, highly optimized COPY or BULK INSERT statement.
        //await context.BulkInsertAsync<Transaction>(transactions!);

        await context.AddRangeAsync(transactions!);
        await context.SaveChangesAsync();
        return transactions;
    }

    public async Task<IEnumerable<Transaction>> GetAll()
    {
        return await context.Transactions.ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetOpenPositionsByUser(Guid userId)
    {
        var openTransactions = await context.Transactions
            .Where(t => t.UserId == userId && t.TransactionType == TransactionType.Buy)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        return openTransactions;
    }
}

