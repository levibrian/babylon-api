using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for calculating rebalancing actions and smart recommendations.
/// </summary>
public class RebalancingService(IPortfolioService portfolioService) : IRebalancingService
{
    /// <summary>
    /// Minimum threshold for rebalancing actions. Actions below this are considered noise.
    /// </summary>
    private const decimal NoiseThreshold = 10m;

    /// <inheritdoc />
    public async Task<RebalancingActionsDto> GetRebalancingActionsAsync(Guid userId)
    {
        var portfolio = await portfolioService.GetPortfolio(userId);
        var totalPortfolioValue = portfolio.TotalInvested;

        if (totalPortfolioValue == 0 || portfolio.Positions.Count == 0)
        {
            return RebalancingActionsDto.Empty;
        }

        var actions = CalculateRebalancingActions(portfolio.Positions, totalPortfolioValue);

        return BuildRebalancingResponse(actions, totalPortfolioValue);
    }

    /// <inheritdoc />
    public async Task<SmartRebalancingResponseDto> GetSmartRecommendationsAsync(
        Guid userId,
        SmartRebalancingRequestDto request)
    {
        ValidateRequest(request);

        var portfolio = await portfolioService.GetPortfolio(userId);

        if (portfolio.TotalInvested == 0 || portfolio.Positions.Count == 0)
        {
            return SmartRebalancingResponseDto.Empty;
        }

        var candidateAssets = GetCandidateAssets(portfolio.Positions, request.OnlyBuyUnderweight);

        if (candidateAssets.Count == 0)
        {
            return SmartRebalancingResponseDto.Empty;
        }

        var selectedAssets = request.MaxSecurities.HasValue
            ? candidateAssets.Take(request.MaxSecurities.Value).ToList()
            : candidateAssets;

        return BuildSmartRebalancingResponse(selectedAssets, request.InvestmentAmount);
    }

    #region Private Helpers

    private static void ValidateRequest(SmartRebalancingRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvestmentAmount <= 0)
        {
            throw new ArgumentException("InvestmentAmount must be greater than 0", nameof(request));
        }

        if (request.MaxSecurities.HasValue && request.MaxSecurities.Value <= 0)
        {
            throw new ArgumentException("MaxSecurities must be greater than 0 if specified", nameof(request));
        }
    }

    private List<RebalancingActionDto> CalculateRebalancingActions(
        IEnumerable<PortfolioPositionDto> positions,
        decimal totalPortfolioValue)
    {
        var actions = new List<RebalancingActionDto>();

        foreach (var position in positions)
        {
            if (!position.TargetAllocationPercentage.HasValue)
                continue;

            var action = CreateRebalancingAction(position, totalPortfolioValue);

            if (action != null && Math.Abs(action.DifferenceValue) >= NoiseThreshold)
            {
                actions.Add(action);
            }
        }

        return actions;
    }

    private static RebalancingActionDto? CreateRebalancingAction(
        PortfolioPositionDto position,
        decimal totalPortfolioValue)
    {
        var currentAllocation = position.CurrentAllocationPercentage ?? 0;
        var targetAllocation = position.TargetAllocationPercentage!.Value;

        var diffValue = (targetAllocation - currentAllocation) / 100m * totalPortfolioValue;

        return new RebalancingActionDto
        {
            Ticker = position.Ticker,
            SecurityName = position.SecurityName,
            CurrentAllocationPercentage = Math.Round(currentAllocation, 2),
            TargetAllocationPercentage = Math.Round(targetAllocation, 2),
            DifferenceValue = Math.Round(diffValue, 2),
            ActionType = diffValue > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell
        };
    }

    private static RebalancingActionsDto BuildRebalancingResponse(
        List<RebalancingActionDto> actions,
        decimal totalPortfolioValue)
    {
        var buyActions = actions.Where(a => a.ActionType == RebalancingActionType.Buy);
        var sellActions = actions.Where(a => a.ActionType == RebalancingActionType.Sell);

        var totalBuyAmount = buyActions.Sum(a => a.DifferenceValue);
        var totalSellAmount = Math.Abs(sellActions.Sum(a => a.DifferenceValue));

        return new RebalancingActionsDto
        {
            Actions = actions.OrderByDescending(a => Math.Abs(a.DifferenceValue)).ToList(),
            TotalPortfolioValue = Math.Round(totalPortfolioValue, 2),
            TotalBuyAmount = Math.Round(totalBuyAmount, 2),
            TotalSellAmount = Math.Round(totalSellAmount, 2),
            NetCashFlow = Math.Round(totalBuyAmount - totalSellAmount, 2)
        };
    }

    private static List<AssetGap> GetCandidateAssets(
        IEnumerable<PortfolioPositionDto> positions,
        bool onlyBuyUnderweight)
    {
        var query = positions
            .Where(p => p.TargetAllocationPercentage.HasValue && p.CurrentAllocationPercentage.HasValue);

        if (onlyBuyUnderweight)
        {
            // Only underweight assets (Target > Current)
            query = query.Where(p => p.TargetAllocationPercentage!.Value > p.CurrentAllocationPercentage!.Value);
        }

        return query
            .Select(p => new AssetGap(
                p,
                p.TargetAllocationPercentage!.Value - (p.CurrentAllocationPercentage ?? 0)))
            .OrderByDescending(x => x.GapScore)
            .ToList();
    }

    private static SmartRebalancingResponseDto BuildSmartRebalancingResponse(
        List<AssetGap> selectedAssets,
        decimal investmentAmount)
    {
        var totalGap = selectedAssets.Sum(a => a.GapScore);

        if (totalGap == 0)
        {
            return SmartRebalancingResponseDto.Empty;
        }

        var recommendations = selectedAssets.Select(asset =>
        {
            var buyAmount = investmentAmount * (asset.GapScore / totalGap);

            return new SmartRebalancingRecommendationDto
            {
                Ticker = asset.Position.Ticker,
                SecurityName = asset.Position.SecurityName,
                CurrentAllocationPercentage = Math.Round(asset.Position.CurrentAllocationPercentage ?? 0, 2),
                TargetAllocationPercentage = Math.Round(asset.Position.TargetAllocationPercentage!.Value, 2),
                GapScore = Math.Round(asset.GapScore, 2),
                RecommendedBuyAmount = Math.Round(buyAmount, 2)
            };
        }).ToList();

        return new SmartRebalancingResponseDto
        {
            Recommendations = recommendations,
            TotalInvestmentAmount = recommendations.Sum(r => r.RecommendedBuyAmount),
            SecuritiesCount = recommendations.Count
        };
    }

    /// <summary>
    /// Represents an asset with its allocation gap.
    /// </summary>
    private sealed record AssetGap(PortfolioPositionDto Position, decimal GapScore);

    #endregion
}

