using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

/// <summary>
/// Backfills RealizedPnL and RealizedPnLPct on Sell transactions that were recorded
/// before these metrics were introduced. Uses FIFO cost basis via RealizedPnLCalculator.
///
/// Idempotent: only processes Sell transactions where RealizedPnL IS NULL.
/// Safe to re-run on every deployment until all historical rows are populated.
/// </summary>
public class RealizedPnlBackfillService(
    ITransactionRepository transactionRepository,
    ILogger<RealizedPnlBackfillService> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogInformation("RealizedPnlBackfillService starting");

        var userIds = await transactionRepository.GetDistinctUserIdsWithUnbackfilledSellsAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            logger.LogDebug("No unbackfilled sell transactions found — skipping");
            return;
        }

        logger.LogInformation(
            "Backfill queued for {UserCount} user(s) with unbackfilled sell transactions", userIds.Count);

        var totalUpdated = 0;
        var usersProcessed = 0;

        foreach (var userId in userIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Cancellation requested — stopping after {UsersProcessed}/{TotalUsers} user(s)",
                    usersProcessed, userIds.Count);
                break;
            }

            try
            {
                var updated = await BackfillForUserAsync(userId, cancellationToken);
                totalUpdated += updated;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to backfill realized PnL for user {UserId} — continuing with remaining users", userId);
            }

            usersProcessed++;
        }

        var elapsed = DateTime.UtcNow - startedAt;
        logger.LogInformation(
            "RealizedPnlBackfillService complete — {UsersProcessed} user(s) processed, {TransactionsUpdated} transaction(s) backfilled in {ElapsedMs}ms",
            usersProcessed, totalUpdated, (int)elapsed.TotalMilliseconds);
    }

    private async Task<int> BackfillForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var allTransactions = (await transactionRepository.GetAllByUser(userId)).ToList();

        if (allTransactions.Count == 0)
        {
            return 0;
        }

        var transactionsToUpdate = new List<Transaction>();
        var skippedNoLots = 0;

        foreach (var securityGroup in allTransactions.GroupBy(t => t.SecurityId))
        {
            var securityTransactions = securityGroup.ToList();

            var transactionDtos = securityTransactions.Select(t => new PortfolioTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Date = t.Date,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees,
                Tax = t.Tax,
                RealizedPnL = t.RealizedPnL,
                RealizedPnLPct = t.RealizedPnLPct
            }).ToList();

            var calculated = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(transactionDtos);

            foreach (var transaction in securityTransactions)
            {
                if (transaction.TransactionType != TransactionType.Sell || transaction.RealizedPnL is not null)
                {
                    continue;
                }

                if (!calculated.TryGetValue(transaction.Id, out var values))
                {
                    continue;
                }

                // Calculator returns (null, null) when no buy lots exist for this sell — skip
                if (values.RealizedPnL is null)
                {
                    skippedNoLots++;
                    continue;
                }

                transaction.RealizedPnL = values.RealizedPnL;
                transaction.RealizedPnLPct = values.RealizedPnLPct;
                transactionsToUpdate.Add(transaction);
            }
        }

        if (transactionsToUpdate.Count > 0)
        {
            await transactionRepository.UpdateBulkAsync(transactionsToUpdate, cancellationToken);

            logger.LogInformation(
                "Backfilled {TransactionsUpdated} sell transaction(s) for user {UserId}",
                transactionsToUpdate.Count, userId);
        }

        if (skippedNoLots > 0)
        {
            logger.LogDebug(
                "Skipped {SkippedCount} sell transaction(s) for user {UserId} — no preceding buy lots for FIFO calculation",
                skippedNoLots, userId);
        }

        return transactionsToUpdate.Count;
    }
}
