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
            var securityTransactions = securityGroup.ToList();

            // Convert to DTO for calculator
            var portfolioTransactions = securityTransactions.Select(t => new PortfolioTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Date = t.Date,
                UpdatedAt = t.UpdatedAt,
                CreatedAt = t.CreatedAt,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees,
                Tax = t.Tax,
                RealizedPnL = t.RealizedPnL,
                RealizedPnLPct = t.RealizedPnLPct
            }).ToList();

            var recalculatedResults = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(portfolioTransactions);

            foreach (var transaction in securityTransactions)
            {
                if (!recalculatedResults.TryGetValue(transaction.Id, out var values))
                {
                    continue;
                }

                if (transaction.RealizedPnL != values.RealizedPnL ||
                    transaction.RealizedPnLPct != values.RealizedPnLPct)
                {
                    transaction.RealizedPnL = values.RealizedPnL;
                    transaction.RealizedPnLPct = values.RealizedPnLPct;
                    await repo.Update(transaction);
                    updatesCount++;
                }
            }
        }

        if (updatesCount > 0)
        {
            _logger.LogInformation("Updated {Count} transactions for User {UserId}", updatesCount, userId);
        }
    }
}
