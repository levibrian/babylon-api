using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Features.Investments.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly BabylonDbContext context;

    public TransactionRepository(BabylonDbContext context)
    {
        this.context = context;
    }

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return await context.Transactions.ToListAsync();
    }
}

