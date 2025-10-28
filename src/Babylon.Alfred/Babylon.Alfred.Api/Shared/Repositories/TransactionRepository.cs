using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using EFCore.BulkExtensions;
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
}

