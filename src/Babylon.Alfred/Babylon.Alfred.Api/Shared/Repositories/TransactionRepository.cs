using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class TransactionRepository(BabylonDbContext context, ILogger<TransactionRepository> logger) : ITransactionRepository
{
    public async Task<Transaction> Add(Transaction transaction)
    {
        logger.LogDatabaseOperation("Create", "Transaction", transaction.Id);
        
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();
        
        logger.LogDatabaseOperation("Created", "Transaction", transaction.Id);
        logger.LogInformation("Transaction created: {TransactionId} for UserId: {UserId}, SecurityId: {SecurityId}", 
            transaction.Id, transaction.UserId, transaction.SecurityId);
        return transaction;
    }

    public async Task<IList<Transaction?>> AddBulk(IList<Transaction?> transactions)
    {
        var count = transactions.Count;
        logger.LogDatabaseOperation("CreateBulk", "Transaction", null, count);
        
        // This method generates a single, highly optimized COPY or BULK INSERT statement.
        //await context.BulkInsertAsync<Transaction>(transactions!);

        await context.AddRangeAsync(transactions!);
        await context.SaveChangesAsync();
        
        logger.LogDatabaseOperation("CreatedBulk", "Transaction", null, count);
        return transactions;
    }

    public async Task<IEnumerable<Transaction>> GetAll()
    {
        logger.LogDatabaseOperation("GetAll", "Transaction");
        
        var transactions = await context.Transactions.ToListAsync();
        
        logger.LogDatabaseOperation("Retrieved", "Transaction", null, transactions.Count);
        return transactions;
    }

    public async Task<IEnumerable<Transaction>> GetOpenPositionsByUser(Guid userId)
    {
        logger.LogInformation("Getting open positions for UserId: {UserId}", userId);
        
        var openTransactions = await context.Transactions
            .Where(t => t.UserId == userId && t.TransactionType == TransactionType.Buy)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        logger.LogDatabaseOperation("RetrievedOpenPositions", "Transaction", null, openTransactions.Count);
        logger.LogInformation("Retrieved {Count} open positions for UserId: {UserId}", openTransactions.Count, userId);
        return openTransactions;
    }
}

