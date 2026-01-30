using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Repositories;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Babylon.Alfred.Worker.Services;

public class RealizedGainsBackfillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RealizedGainsBackfillService> _logger;

    public RealizedGainsBackfillService(IServiceScopeFactory scopeFactory, ILogger<RealizedGainsBackfillService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Realized Gains Backfill Job...");

        using var scope = _scopeFactory.CreateScope();
        var transactionRepository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        
        try 
        {
            await BackfillForAllUsers(transactionRepository, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during PnL Backfill.");
        }

        _logger.LogInformation("Realized Gains Backfill Job Finished.");
    }

    private async Task BackfillForAllUsers(ITransactionRepository transactionRepository, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Babylon.Alfred.Api.Shared.Data.BabylonDbContext>();
        
        var userIds = await dbContext.Transactions
            .Select(t => t.UserId)
            .Distinct()
            .Where(id => id.HasValue)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} users to process.", userIds.Count);

        foreach (var userId in userIds)
        {
            if (userId == null) continue;
            await ProcessUser(userId.Value, scope.ServiceProvider, cancellationToken);
        }
    }

    private async Task ProcessUser(Guid userId, IServiceProvider services, CancellationToken stoppingToken)
    {
        var repo = services.GetRequiredService<ITransactionRepository>();
        var userTransactions = await repo.GetAllByUser(userId);

        if (!userTransactions.Any()) return;

        // Group by Security to process independent positions
        var transactionsBySecurity = userTransactions.GroupBy(t => t.SecurityId);
        var updatesCount = 0;

        foreach (var securityGroup in transactionsBySecurity)
        {
            var securityTransactions = securityGroup.OrderBy(t => t.Date).ThenBy(t => t.UpdatedAt).ToList();
            
            // Convert to DTO for calculator
            var portfolioTransactions = securityTransactions.Select(t => new PortfolioTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Date = t.Date,
                UpdatedAt = t.UpdatedAt,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees,
                Tax = t.Tax
            }).ToList();

            // We need to replay history and capture state at each step
            // PortfolioCalculator.CalculatePositionMetrics gives final state. 
            // We need intermediate state.
            // So we must replicate the logic of PortfolioCalculator manually here 
            // OR iterate incrementally.
            
            decimal currentShares = 0;
            decimal currentCostBasis = 0;

            for (int i = 0; i < securityTransactions.Count; i++)
            {
                var txn = securityTransactions[i];
                var dto = portfolioTransactions[i];

                if (txn.TransactionType == TransactionType.Sell)
                {
                    // Check if it needs backfill or if we should blindly recalculate?
                    // User said "fix ... where ... are null".
                    // But for consistency, if we are replaying, we might as well verify? 
                    // Let's only update matches condition.
                    
                    if (txn.RealizedPnL == null)
                    {
                        var averageSharePrice = currentShares > 0 ? currentCostBasis / currentShares : 0;
                        
                        if (averageSharePrice > 0)
                        {
                            var realizedPnL = (txn.SharePrice - averageSharePrice) * txn.SharesQuantity;
                            var realizedPnLPct = ((txn.SharePrice - averageSharePrice) / averageSharePrice) * 100;

                            txn.RealizedPnL = realizedPnL;
                            txn.RealizedPnLPct = realizedPnLPct;
                            
                            // We need to update this transaction in the DB.
                            // Since we loaded `userTransactions` from Repo, are they tracked? 
                            // `GetAllByUser` usually returns AsNoTracking for read performance in typical repos, 
                            // but let's assume valid tracking or we explicitly update.
                            // I'll call repo.Update() for safety.
                            
                            await repo.Update(txn);
                            updatesCount++;
                        }
                    }
                }

                // Update running Cost Basis for NEXT validation
                // We can reuse the static helper methods from PortfolioCalculator if they are public
                // CalculateCostBasis takes a list... 
                // Let's reuse the per-transaction logic if exposed, otherwise logic match:
                
                // Logic from PortfolioCalculator.cs (viewed previously):
                /*
                 TransactionType.Buy => ProcessBuyTransaction...
                 TransactionType.Sell => ProcessSellTransaction...
                */
                
                // Since the helper methods in PortfolioCalculator are likely private (Need to verify),
                // I will implement the simple accumulation logic here matching the Calculator.
                
                if (txn.TransactionType == TransactionType.Buy)
                {
                     currentShares += txn.SharesQuantity;
                     currentCostBasis += (txn.SharesQuantity * txn.SharePrice) + txn.Fees;
                }
                else if (txn.TransactionType == TransactionType.Sell)
                {
                    if (currentShares > 0)
                    {
                        var avgCost = currentCostBasis / currentShares;
                        var sharesToSell = Math.Min(txn.SharesQuantity, currentShares);
                        var costToRemove = sharesToSell * avgCost;
                        
                        currentShares -= sharesToSell;
                        currentCostBasis -= costToRemove;
                    }
                    if (currentShares == 0) currentCostBasis = 0;
                }
                else if (txn.TransactionType == TransactionType.Split)
                {
                     // Split logic: newShares = currentShares * quantity. Cost basis same.
                     if (currentShares > 0 && txn.SharesQuantity > 0)
                     {
                         currentShares *= txn.SharesQuantity;
                     }
                }
            }
        }

        if (updatesCount > 0)
        {
            _logger.LogInformation("Updated {Count} transactions for User {UserId}", updatesCount, userId);
        }
    }
}
