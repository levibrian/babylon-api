using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioService(
    ITransactionRepository transactionRepository,
    ISecurityRepository securityRepository,
    IMarketPriceService marketPriceService,
    IAllocationStrategyService allocationStrategyService) : IPortfolioService
{
    public async Task<PortfolioResponse> GetPortfolio(Guid userId)
    {
        var effectiveUserId = userId; // No fallback
        // Get Buy transactions to determine which securities have open positions
        var buyTransactions = (await transactionRepository.GetOpenPositionsByUser(effectiveUserId)).ToList();

        if (buyTransactions.Count == 0)
        {
            return new PortfolioResponse
            {
                Positions = [],
                TotalInvested = 0
            };
        }

        // Get SecurityIds that have open positions
        var securityIdsWithPositions = buyTransactions.Select(t => t.SecurityId).Distinct().ToList();

        // Get ALL transactions (Buy, Sell, Dividend) for securities with open positions
        var allTransactions = (await transactionRepository.GetAllByUser(effectiveUserId))
            .Where(t => securityIdsWithPositions.Contains(t.SecurityId))
            .ToList();

        // Group by SecurityId instead of Ticker
        var groupedTransactions = allTransactions.GroupBy(t => t.SecurityId).ToList();
        var positions = await CreatePositionsAsync(groupedTransactions, effectiveUserId);

        // Order by total invested (descending), so largest positions appear first
        var orderedPositions = positions
            .OrderByDescending(p => p.CurrentMarketValue ?? p.TotalInvested)
            .ToList();

        // Calculate portfolio totals
        var totalInvested = orderedPositions.Sum(p => p.TotalInvested);
        var totalMarketValue = orderedPositions
            .Where(p => p.CurrentMarketValue.HasValue)
            .Sum(p => p.CurrentMarketValue!.Value);

        decimal? totalUnrealizedPnL = null;
        decimal? totalUnrealizedPnLPercentage = null;

        if (totalMarketValue > 0 && totalInvested > 0)
        {
            totalUnrealizedPnL = Math.Round(totalMarketValue - totalInvested, 2);
            totalUnrealizedPnLPercentage = Math.Round((totalUnrealizedPnL.Value / totalInvested) * 100, 2);
        }

        return new PortfolioResponse
        {
            Positions = orderedPositions,
            TotalInvested = totalInvested,
            TotalMarketValue = totalMarketValue > 0 ? totalMarketValue : null,
            TotalUnrealizedPnL = totalUnrealizedPnL,
            TotalUnrealizedPnLPercentage = totalUnrealizedPnLPercentage
        };
    }

    /// <summary>
    /// Creates position DTOs from grouped transactions.
    /// Fetches all securities, market prices, and allocation strategies, then processes everything in memory.
    /// </summary>
    private async Task<List<PortfolioPositionDto>> CreatePositionsAsync(
        List<IGrouping<Guid, Transaction>> groupedTransactions,
        Guid userId)
    {
        // Fetch all securities by SecurityId
        var securityIds = groupedTransactions.Select(g => g.Key).ToList();
        var securities = await securityRepository.GetByIdsAsync(securityIds);
        var securitiesLookup = securities.ToDictionary(s => s.Id, s => s);

        // Get tickers for market price lookup (for display purposes only, not used for rebalancing)
        var tickers = securities.Select(s => s.Ticker).ToList();
        var marketPrices = await marketPriceService.GetCurrentPricesAsync(tickers);

        // Fetch target allocations
        var allocationDtos = await allocationStrategyService.GetTargetAllocationsAsync(userId);
        var targetAllocations = allocationDtos.ToDictionary(a => a.Ticker, a => a.TargetPercentage);

        // Calculate total portfolio value using total invested (cost basis) - only Buy transactions count toward invested amount
        // Dividends are income, not investment, so they shouldn't be included in total invested
        var totalPortfolioValue = groupedTransactions.Sum(group =>
            group.Where(t => t.TransactionType == TransactionType.Buy).Sum(t => t.TotalAmount));

        // Process all positions in memory
        return groupedTransactions
            .Select(group => CreatePosition(
                group,
                securitiesLookup.GetValueOrDefault(group.Key),
                marketPrices,
                targetAllocations,
                totalPortfolioValue))
            .ToList();
    }

    /// <summary>
    /// Creates a single position DTO from a group of transactions, security information, market prices, and allocation data.
    /// </summary>
    private static PortfolioPositionDto CreatePosition(
        IGrouping<Guid, Transaction> transactionGroup,
        Security? security,
        Dictionary<string, decimal> marketPrices,
        Dictionary<string, decimal> targetAllocations,
        decimal totalPortfolioValue)
    {
        var ticker = security?.Ticker ?? string.Empty;
        var positionTransactions = MapToTransactionDtos(transactionGroup);
        var (totalShares, averageSharePrice) = PortfolioCalculator.CalculatePositionMetrics(positionTransactions);
        // Total invested should only include Buy transactions (cost basis), not dividends or sells
        var totalInvested = transactionGroup.Where(t => t.TransactionType == TransactionType.Buy).Sum(t => t.TotalAmount);

        // Calculate market value for display purposes only
        var currentPrice = marketPrices.GetValueOrDefault(ticker, 0);
        var currentMarketValue = totalShares * currentPrice;

        // Calculate allocation using total invested (cost basis) instead of market value
        var currentAllocation = PortfolioCalculator.CalculateCurrentAllocationPercentage(totalInvested, totalPortfolioValue);

        // Get target allocation if set
        var targetAllocation = targetAllocations.GetValueOrDefault(ticker);
        var hasTargetAllocation = targetAllocations.ContainsKey(ticker);

        // Calculate deviation and rebalancing amount if target allocation exists
        // Using total invested (cost basis) for rebalancing calculations
        decimal? allocationDeviation = null;
        decimal? rebalancingAmount = null;
        var rebalancingStatus = RebalancingStatus.Balanced;

        if (hasTargetAllocation)
        {
            allocationDeviation = currentAllocation - targetAllocation;
            rebalancingAmount = PortfolioCalculator.CalculateRebalancingAmount(totalInvested, targetAllocation, totalPortfolioValue);
            rebalancingStatus = PortfolioCalculator.DetermineRebalancingStatus(currentAllocation, targetAllocation);
        }

        // Calculate P&L (only if we have market value)
        decimal? unrealizedPnL = null;
        decimal? unrealizedPnLPercentage = null;

        if (currentMarketValue > 0 && totalInvested > 0)
        {
            unrealizedPnL = currentMarketValue - totalInvested;
            unrealizedPnLPercentage = (unrealizedPnL / totalInvested) * 100;
        }

        // Return only the last 5 transactions for display (already ordered by date descending)
        var displayTransactions = positionTransactions.Take(5).ToList();

        return new PortfolioPositionDto
        {
            Ticker = ticker,
            SecurityName = security?.SecurityName ?? ticker,
            SecurityType = security?.SecurityType ?? SecurityType.Stock,
            TotalInvested = totalInvested,
            TotalShares = totalShares,
            AverageSharePrice = averageSharePrice,
            Sector = security?.Sector,
            Industry = security?.Industry,
            Geography = security?.Geography,
            MarketCap = security?.MarketCap,
            CurrentMarketValue = currentMarketValue > 0 ? currentMarketValue : null,
            UnrealizedPnL = unrealizedPnL.HasValue ? Math.Round(unrealizedPnL.Value, 2) : null,
            UnrealizedPnLPercentage = unrealizedPnLPercentage.HasValue ? Math.Round(unrealizedPnLPercentage.Value, 2) : null,
            CurrentAllocationPercentage = totalPortfolioValue > 0 ? currentAllocation : null,
            TargetAllocationPercentage = hasTargetAllocation ? targetAllocation : null,
            AllocationDeviation = allocationDeviation,
            RebalancingAmount = rebalancingAmount,
            RebalancingStatus = rebalancingStatus,
            Transactions = displayTransactions
        };
    }

    /// <summary>
    /// Maps domain transactions to DTOs, ordered by date descending (newest first).
    /// </summary>
    private static List<PortfolioTransactionDto> MapToTransactionDtos(
        IEnumerable<Transaction> transactions)
    {
        return transactions
            .Select(t => new PortfolioTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Date = t.Date,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees,
                Tax = t.Tax
                // TotalAmount is computed, so we don't need to set it explicitly
            })
            .OrderByDescending(t => t.Date)
            .ToList();
    }

}
