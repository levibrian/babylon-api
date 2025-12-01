using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Microsoft.EntityFrameworkCore;

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
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        logger.LogDatabaseOperation("RetrievedOpenPositions", "Transaction", null, openTransactions.Count);
        logger.LogInformation("Retrieved {Count} open positions for UserId: {UserId}", openTransactions.Count, userId);
        return openTransactions;
    }

    public async Task<IEnumerable<Transaction>> GetAllByUser(Guid userId)
    {
        logger.LogInformation("Getting all transactions for UserId: {UserId}", userId);

        var transactions = await context.Transactions
            .Include(t => t.Security)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.UpdatedAt)
            .ToListAsync();

        logger.LogDatabaseOperation("RetrievedAllByUser", "Transaction", null, transactions.Count);
        var transactionTypes = transactions.GroupBy(t => t.TransactionType)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        logger.LogInformation("Retrieved {Count} transactions for UserId: {UserId}. Breakdown: {Types}",
            transactions.Count,
            userId,
            string.Join(", ", transactionTypes));
        return transactions;
    }

    public async Task<Transaction?> GetById(Guid transactionId, Guid userId)
    {
        logger.LogInformation("Getting transaction {TransactionId} for UserId: {UserId}", transactionId, userId);

        var transaction = await context.Transactions
            .Include(t => t.Security)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            logger.LogWarning("Transaction {TransactionId} not found for UserId: {UserId}", transactionId, userId);
        }
        else
        {
            logger.LogDatabaseOperation("Retrieved", "Transaction", transactionId);
        }

        return transaction;
    }

    public async Task<Transaction> Update(Transaction transaction)
    {
        logger.LogDatabaseOperation("Update", "Transaction", transaction.Id);

        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();

        logger.LogDatabaseOperation("Updated", "Transaction", transaction.Id);
        logger.LogInformation("Transaction updated: {TransactionId} for UserId: {UserId}, SecurityId: {SecurityId}",
            transaction.Id, transaction.UserId, transaction.SecurityId);
        return transaction;
    }

    public async Task Delete(Guid transactionId, Guid userId)
    {
        logger.LogInformation("Deleting transaction {TransactionId} for UserId: {UserId}", transactionId, userId);

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            logger.LogWarning("Transaction {TransactionId} not found for UserId: {UserId} - cannot delete", transactionId, userId);
            throw new InvalidOperationException($"Transaction {transactionId} not found for user {userId}");
        }

        logger.LogDatabaseOperation("Delete", "Transaction", transactionId);

        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync();

        logger.LogDatabaseOperation("Deleted", "Transaction", transactionId);
        logger.LogInformation("Transaction deleted: {TransactionId} for UserId: {UserId}", transactionId, userId);
    }
}

