using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class PortfolioService(
    ITransactionRepository transactionRepository,
    ISecurityRepository securityRepository,
    IMarketPriceService marketPriceService,
    IAllocationStrategyService allocationStrategyService,
    ICashBalanceService cashBalanceService) : IPortfolioService
{
    public async Task<PortfolioResponse> GetPortfolio(Guid userId)
    {
        var effectiveUserId = userId; // No fallback
        var cashAmount = await cashBalanceService.GetBalanceAsync(effectiveUserId);

        // Get Buy transactions to determine which securities have open positions
        var buyTransactions = (await transactionRepository.GetOpenPositionsByUser(effectiveUserId)).ToList();

        var positions = new List<PortfolioPositionDto>();
        decimal totalInvested = 0;
        decimal totalAssetsValue = 0;

        if (buyTransactions.Count > 0)
        {
            // Get SecurityIds that have open positions
            var securityIdsWithPositions = buyTransactions.Select(t => t.SecurityId).Distinct().ToList();

            // Get ALL transactions (Buy, Sell, Dividend) for securities with open positions
            var allTransactions = (await transactionRepository.GetAllByUser(effectiveUserId))
                .Where(t => securityIdsWithPositions.Contains(t.SecurityId))
                .ToList();

            // Group by SecurityId instead of Ticker
            var groupedTransactions = allTransactions.GroupBy(t => t.SecurityId).ToList();
            positions = await CreatePositionsAsync(groupedTransactions, effectiveUserId, cashAmount);

            // Order by total invested (descending), so largest positions appear first
            positions = positions
                .OrderByDescending(p => p.CurrentMarketValue ?? p.TotalInvested)
                .ToList();

            totalInvested = positions.Sum(p => p.TotalInvested);
            totalAssetsValue = positions.Sum(p => p.CurrentMarketValue ?? p.TotalInvested);
        }

        var totalValue = totalAssetsValue + cashAmount;

        // Add cash as a virtual position if it exists
        if (cashAmount > 0)
        {
            var cashAllocation = totalValue > 0 ? (cashAmount / totalValue) * 100 : 0;
            positions.Add(new PortfolioPositionDto
            {
                Ticker = "CASH",
                SecurityName = "Cash",
                SecurityType = SecurityType.Cash,
                TotalInvested = 0, // Cash is not an invested asset in the transaction sense
                TotalShares = cashAmount,
                AverageSharePrice = 1,
                CurrentMarketValue = cashAmount,
                CurrentAllocationPercentage = Math.Round(cashAllocation, 2),
                TargetAllocationPercentage = 0, // Manual adjustment if needed
                AllocationDeviation = 0,
                RebalancingAmount = 0,
                RebalancingStatus = RebalancingStatus.Balanced
            });

            // Re-order to keep cash usually at the bottom or maintain descending order
            positions = positions.OrderByDescending(p => p.CurrentMarketValue ?? p.TotalInvested).ToList();
        }

        decimal? totalUnrealizedPnL = null;
        decimal? totalUnrealizedPnLPercentage = null;

        if (totalAssetsValue > 0 && totalInvested > 0)
        {
            totalUnrealizedPnL = Math.Round(totalAssetsValue - totalInvested, 2);
            totalUnrealizedPnLPercentage = Math.Round((totalUnrealizedPnL.Value / totalInvested) * 100, 2);
        }

        return new PortfolioResponse
        {
            Positions = positions,
            CashAmount = cashAmount,
            TotalInvested = totalInvested,
            TotalMarketValue = totalValue > 0 ? totalValue : null,
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
        Guid userId,
        decimal cashAmount)
    {
        // Fetch all securities by SecurityId
        var securityIds = groupedTransactions.Select(g => g.Key).ToList();
        var securities = await securityRepository.GetByIdsAsync(securityIds);
        var securitiesLookup = securities.ToDictionary(s => s.Id, s => s);

        // Get tickers for market price lookup
        var tickers = securities.Select(s => s.Ticker).ToList();
        var marketPrices = await marketPriceService.GetCurrentPricesAsync(tickers);

        // Fetch target allocations
        var allocationDtos = await allocationStrategyService.GetTargetAllocationsAsync(userId);
        var targetAllocations = allocationDtos.ToDictionary(a => a.Ticker, a => a.TargetPercentage);

        // First pass: calculate metrics and market value for each position
        var positionData = groupedTransactions.Select(group =>
        {
            var security = securitiesLookup.GetValueOrDefault(group.Key);
            var ticker = security?.Ticker ?? string.Empty;
            var positionTransactions = MapToTransactionDtos(group);
            var (totalShares, averageSharePrice, costBasis) = PortfolioCalculator.CalculatePositionMetrics(positionTransactions);
            var totalInvested = costBasis;
            var currentPrice = marketPrices.GetValueOrDefault(ticker, 0);
            var currentMarketValue = totalShares * currentPrice;

            return new
            {
                Group = group,
                Security = security,
                Ticker = ticker,
                TotalShares = totalShares,
                AverageSharePrice = averageSharePrice,
                TotalInvested = totalInvested,
                CurrentMarketValue = currentMarketValue
            };
        })
        .Where(p => p.TotalShares > 0) // Only include positions with open shares
        .ToList();

        // Calculate total portfolio values
        // Use Market Value if available, otherwise fallback to Invested for each position to get the most accurate "real value"
        var totalAssetsValue = positionData.Sum(p => p.CurrentMarketValue > 0 ? p.CurrentMarketValue : p.TotalInvested);
        var totalPortfolioValue = totalAssetsValue + cashAmount;

        // Second pass: create final DTOs with allocation and rebalancing data
        return positionData.Select(p =>
        {
            var targetAllocation = targetAllocations.GetValueOrDefault(p.Ticker, 0m);

            // Use current market value for allocation if it exists, otherwise use total invested (cost basis)
            var positionBaseValue = p.CurrentMarketValue > 0 ? p.CurrentMarketValue : p.TotalInvested;

            // Calculate allocation using "real value" (market value) instead of cost basis
            var currentAllocation = totalPortfolioValue > 0
                ? PortfolioCalculator.CalculateCurrentAllocationPercentage(positionBaseValue, totalPortfolioValue)
                : 0m;

            var allocationDeviation = currentAllocation - targetAllocation;
            var rebalancingAmount = totalPortfolioValue > 0
                ? PortfolioCalculator.CalculateRebalancingAmount(positionBaseValue, targetAllocation, totalPortfolioValue)
                : 0m;
            var rebalancingStatus = PortfolioCalculator.DetermineRebalancingStatus(currentAllocation, targetAllocation);

            // Calculate P&L
            decimal? unrealizedPnL = null;
            decimal? unrealizedPnLPercentage = null;

            if (p.CurrentMarketValue > 0 && p.TotalInvested > 0)
            {
                unrealizedPnL = p.CurrentMarketValue - p.TotalInvested;
                unrealizedPnLPercentage = (unrealizedPnL / p.TotalInvested) * 100;
            }

            return new PortfolioPositionDto
            {
                Ticker = p.Ticker,
                SecurityName = p.Security?.SecurityName ?? p.Ticker,
                SecurityType = p.Security?.SecurityType ?? SecurityType.Stock,
                TotalInvested = p.TotalInvested,
                TotalShares = p.TotalShares,
                AverageSharePrice = p.AverageSharePrice,
                Sector = p.Security?.Sector,
                Industry = p.Security?.Industry,
                Geography = p.Security?.Geography,
                MarketCap = p.Security?.MarketCap,
                CurrentMarketValue = p.CurrentMarketValue > 0 ? p.CurrentMarketValue : null,
                UnrealizedPnL = unrealizedPnL.HasValue ? Math.Round(unrealizedPnL.Value, 2) : null,
                UnrealizedPnLPercentage = unrealizedPnLPercentage.HasValue ? Math.Round(unrealizedPnLPercentage.Value, 2) : null,
                CurrentAllocationPercentage = totalPortfolioValue > 0 ? Math.Round(currentAllocation, 2) : null,
                TargetAllocationPercentage = targetAllocation,
                AllocationDeviation = Math.Round(allocationDeviation, 2),
                RebalancingAmount = Math.Round(rebalancingAmount, 2),
                RebalancingStatus = rebalancingStatus
            };
        }).ToList();
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
                UpdatedAt = t.UpdatedAt,
                SharesQuantity = t.SharesQuantity,
                SharePrice = t.SharePrice,
                Fees = t.Fees,
                Tax = t.Tax
                // TotalAmount is computed, so we don't need to set it explicitly
            })
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.UpdatedAt)
            .ToList();
    }

}
