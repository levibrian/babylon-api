using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Babylon.Alfred.Worker.Services;

/// <summary>
/// Service for creating hourly portfolio snapshots.
/// Calculates portfolio metrics for each user and persists them for historical tracking and intraday charts.
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
                    var snapshot = await CreateSnapshotForUserAsync(userId);

                    if (snapshot != null)
                    {
                        await snapshotRepository.AddSnapshotAsync(snapshot);
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
    private async Task<PortfolioSnapshot?> CreateSnapshotForUserAsync(Guid userId)
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
            var transactionDtos = MapToTransactionDtos(transactions);
            var (totalShares, _) = PortfolioCalculator.CalculateCostBasis(transactionDtos);

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
            TotalInvested = Math.Round(totalInvested, 2),
            TotalMarketValue = Math.Round(totalMarketValue, 2),
            UnrealizedPnL = Math.Round(unrealizedPnL, 2),
            UnrealizedPnLPercentage = Math.Round(unrealizedPnLPercentage, 4)
        };
    }

    /// <summary>
    /// Maps Transaction entities to PortfolioTransactionDto for use with PortfolioCalculator.
    /// </summary>
    private static List<PortfolioTransactionDto> MapToTransactionDtos(List<Transaction> transactions)
    {
        return transactions.Select(t => new PortfolioTransactionDto
        {
            Id = t.Id,
            TransactionType = t.TransactionType,
            Date = t.Date,
            SharesQuantity = t.SharesQuantity,
            SharePrice = t.SharePrice,
            Fees = t.Fees,
            Tax = t.Tax
        }).ToList();
    }
}

