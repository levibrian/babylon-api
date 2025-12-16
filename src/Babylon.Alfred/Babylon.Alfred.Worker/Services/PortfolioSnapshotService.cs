using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

/// <summary>
/// Service for creating daily portfolio snapshots.
/// Calculates portfolio metrics for each user and persists them for historical tracking.
/// </summary>
public class PortfolioSnapshotService(
    IPortfolioSnapshotRepository snapshotRepository,
    ITransactionRepository transactionRepository,
    IMarketPriceRepository marketPriceRepository,
    ISecurityRepository securityRepository,
    ILogger<PortfolioSnapshotService> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting portfolio snapshot job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Get all user IDs that have portfolios
            var userIds = await snapshotRepository.GetUserIdsWithPortfoliosAsync();

            if (userIds.Count == 0)
            {
                logger.LogInformation("No users with portfolios found. Skipping snapshot creation.");
                return;
            }

            logger.LogInformation("Found {Count} users with portfolios to snapshot", userIds.Count);

            var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var successCount = 0;
            var skipCount = 0;

            foreach (var userId in userIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Portfolio snapshot job cancelled");
                    break;
                }

                try
                {
                    var snapshot = await CreateSnapshotForUserAsync(userId, snapshotDate);
                    
                    if (snapshot != null)
                    {
                        await snapshotRepository.UpsertSnapshotAsync(snapshot);
                        successCount++;
                        logger.LogDebug(
                            "Snapshot created for user {UserId}: Value={MarketValue:C}, P&L={PnL:C} ({PnLPct:F2}%)",
                            userId, snapshot.TotalMarketValue, snapshot.UnrealizedPnL, snapshot.UnrealizedPnLPercentage);
                    }
                    else
                    {
                        skipCount++;
                        logger.LogDebug("Skipped snapshot for user {UserId} - no market value available", userId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create snapshot for user {UserId}", userId);
                }
            }

            logger.LogInformation(
                "Portfolio snapshot completed. Created: {Success}, Skipped: {Skip}, Total Users: {Total}",
                successCount, skipCount, userIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in portfolio snapshot job");
            throw;
        }
    }

    /// <summary>
    /// Creates a portfolio snapshot for a single user.
    /// Returns null if market values are not available.
    /// </summary>
    private async Task<PortfolioSnapshot?> CreateSnapshotForUserAsync(Guid userId, DateOnly snapshotDate)
    {
        // Get all transactions for the user
        var allTransactions = (await transactionRepository.GetAllByUser(userId)).ToList();
        
        if (allTransactions.Count == 0)
        {
            return null;
        }

        // Group transactions by security
        var transactionsBySecurityId = allTransactions.GroupBy(t => t.SecurityId).ToList();
        
        // Get all security IDs
        var securityIds = transactionsBySecurityId.Select(g => g.Key).ToList();
        
        // Get securities to map to tickers for market prices
        var securities = await securityRepository.GetByIdsAsync(securityIds);
        var securityLookup = securities.ToDictionary(s => s.Id, s => s);
        
        // Get market prices
        var tickers = securities.Select(s => s.Ticker).ToList();
        var marketPrices = await marketPriceRepository.GetByTickersAsync(tickers);

        decimal totalInvested = 0;
        decimal totalMarketValue = 0;
        var hasMarketPrices = false;

        foreach (var group in transactionsBySecurityId)
        {
            var securityId = group.Key;
            var transactions = group.ToList();

            // Calculate total invested (only Buy transactions count)
            var invested = transactions
                .Where(t => t.TransactionType == TransactionType.Buy)
                .Sum(t => t.TotalAmount);
            totalInvested += invested;

            // Calculate total shares using weighted average cost method
            var (totalShares, _) = CalculateCostBasis(transactions);

            // Skip if no shares (position was fully sold)
            if (totalShares <= 0)
            {
                continue;
            }

            // Get market price for this security
            if (!securityLookup.TryGetValue(securityId, out var security))
            {
                continue;
            }

            if (marketPrices.TryGetValue(security.Ticker, out var marketPrice))
            {
                totalMarketValue += totalShares * marketPrice.Price;
                hasMarketPrices = true;
            }
        }

        // Skip snapshot if we don't have any market prices
        if (!hasMarketPrices || totalMarketValue == 0)
        {
            return null;
        }

        var unrealizedPnL = totalMarketValue - totalInvested;
        var unrealizedPnLPercentage = totalInvested > 0 
            ? (unrealizedPnL / totalInvested) * 100 
            : 0;

        return new PortfolioSnapshot
        {
            UserId = userId,
            SnapshotDate = snapshotDate,
            TotalInvested = Math.Round(totalInvested, 2),
            TotalMarketValue = Math.Round(totalMarketValue, 2),
            UnrealizedPnL = Math.Round(unrealizedPnL, 2),
            UnrealizedPnLPercentage = Math.Round(unrealizedPnLPercentage, 4)
        };
    }

    /// <summary>
    /// Calculates the cost basis and total shares using weighted average cost method.
    /// Processes transactions chronologically.
    /// </summary>
    private static (decimal totalShares, decimal costBasis) CalculateCostBasis(List<Transaction> transactions)
    {
        decimal totalShares = 0;
        decimal costBasis = 0;

        // Order by date ascending to process transactions chronologically
        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        foreach (var t in orderedTransactions)
        {
            switch (t.TransactionType)
            {
                case TransactionType.Buy:
                    totalShares += t.SharesQuantity;
                    costBasis += (t.SharesQuantity * t.SharePrice) + t.Fees;
                    break;
                    
                case TransactionType.Sell:
                    if (totalShares > 0)
                    {
                        var avgCost = costBasis / totalShares;
                        var sharesToSell = Math.Min(t.SharesQuantity, totalShares);
                        costBasis -= sharesToSell * avgCost;
                        totalShares -= sharesToSell;
                        
                        if (totalShares == 0)
                        {
                            costBasis = 0;
                        }
                    }
                    break;
                    
                case TransactionType.Split:
                    if (totalShares > 0 && t.SharesQuantity > 0)
                    {
                        totalShares *= t.SharesQuantity;
                    }
                    break;
                    
                // Dividends don't affect cost basis or share count
            }
        }

        return (totalShares, costBasis);
    }
}

