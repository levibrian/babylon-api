using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;
using Babylon.Alfred.Api.Features.Investments.Options;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;
using Microsoft.Extensions.Options;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Service for generating timed rebalancing actions (actions + timing).
/// P1 implementation is built up incrementally (timing + pairing + optional AI optimization).
/// </summary>
public class TimedRebalancingActionsService(
    IPortfolioService portfolioService,
    IMarketPriceService marketPriceService,
    IHistoricalPriceService historicalPriceService,
    IRebalancingOptimizer rebalancingOptimizer,
    ILogger<TimedRebalancingActionsService> logger,
    IOptions<TimedRebalancingActionsOptions> options)
    : ITimedRebalancingActionsService
{
    public async Task<TimedRebalancingActionsResponseDto> GetTimedActionsAsync(
        Guid userId,
        decimal? investmentAmount,
        int? maxActions,
        bool useAi = false)
    {
        var actionsOptions = options.Value;

        var portfolio = await portfolioService.GetPortfolio(userId);
        var totalPortfolioValue = portfolio.TotalMarketValue ?? portfolio.TotalInvested;
        var cashAvailable = portfolio.CashAmount + (investmentAmount ?? 0);

        if (totalPortfolioValue <= 0 || portfolio.Positions.Count == 0)
        {
            return new TimedRebalancingActionsResponseDto
            {
                TotalPortfolioValue = 0,
                CashAvailable = 0,
                TotalBuyAmount = 0,
                TotalSellAmount = 0,
                NetCashFlow = 0,
                BuyPercentileThreshold1Y = actionsOptions.BuyPercentileThreshold1Y,
                SellPercentileThreshold1Y = actionsOptions.SellPercentileThreshold1Y,
                GeneratedAtUtc = DateTime.UtcNow,
                Buys = [],
                Sells = [],
                AiApplied = false
            };
        }

        var effectiveMaxActions = maxActions.GetValueOrDefault(actionsOptions.DefaultMaxActions);
        if (effectiveMaxActions <= 0)
        {
            effectiveMaxActions = actionsOptions.DefaultMaxActions;
        }

        var rawCandidates = BuildRawCandidates(portfolio.Positions, totalPortfolioValue, actionsOptions.NoiseThreshold);

        // Limit tickers we fetch history for (rate limiting protection)
        var tickersForTiming = rawCandidates
            .OrderByDescending(c => Math.Abs(c.SignedAmount))
            .Select(c => c.Position.Ticker)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(actionsOptions.MaxTickersForTiming)
            .ToList();

        var currentPrices = tickersForTiming.Count > 0
            ? await marketPriceService.GetCurrentPricesAsync(tickersForTiming)
            : new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var timingPercentiles = await FetchTimingPercentiles1YAsync(
            tickersForTiming,
            currentPrices,
            rawCandidates.Select(c => c.Position).ToList());

        var (sells, buys) = ApplyTimingFilterAndBuildActions(
            rawCandidates,
            timingPercentiles,
            currentPrices,
            actionsOptions,
            effectiveMaxActions);

        // Pairing logic: balance sells → buys to achieve net cashflow ≈ 0 (or use available cash)
        (sells, buys) = ApplyPairing(sells, buys, cashAvailable, actionsOptions.NoiseThreshold);

        // Optional AI optimization (Gemini)
        string? aiSummary = null;
        if (useAi && rebalancingOptimizer.IsEnabled && (sells.Count > 0 || buys.Count > 0))
        {
            var optimizerResult = await TryAiOptimizationAsync(
                sells, buys, rawCandidates, timingPercentiles, currentPrices,
                totalPortfolioValue, cashAvailable, actionsOptions, effectiveMaxActions);

            if (optimizerResult.HasValue)
            {
                (sells, buys, aiSummary) = optimizerResult.Value;
            }
        }

        var totalBuy = buys.Sum(a => a.Amount);
        var totalSell = sells.Sum(a => a.Amount);

        var aiApplied = aiSummary != null;

        return new TimedRebalancingActionsResponseDto
        {
            TotalPortfolioValue = Math.Round(totalPortfolioValue, 2),
            CashAvailable = Math.Round(cashAvailable, 2),
            TotalBuyAmount = Math.Round(totalBuy, 2),
            TotalSellAmount = Math.Round(totalSell, 2),
            NetCashFlow = Math.Round(totalBuy - totalSell, 2),
            BuyPercentileThreshold1Y = actionsOptions.BuyPercentileThreshold1Y,
            SellPercentileThreshold1Y = actionsOptions.SellPercentileThreshold1Y,
            GeneratedAtUtc = DateTime.UtcNow,
            Sells = sells,
            Buys = buys,
            AiApplied = aiApplied,
            AiSummary = aiSummary
        };
    }

    private async Task<(List<TimedRebalancingActionDto> Sells, List<TimedRebalancingActionDto> Buys, string? Summary)?> TryAiOptimizationAsync(
        List<TimedRebalancingActionDto> sells,
        List<TimedRebalancingActionDto> buys,
        List<RawCandidate> rawCandidates,
        Dictionary<string, decimal> timingPercentiles,
        Dictionary<string, decimal> currentPrices,
        decimal totalPortfolioValue,
        decimal cashAvailable,
        TimedRebalancingActionsOptions actionsOptions,
        int maxActions)
    {
        try
        {
            var request = BuildOptimizerRequest(
                sells, buys, rawCandidates, timingPercentiles, currentPrices,
                totalPortfolioValue, cashAvailable, actionsOptions, maxActions);

            var result = await rebalancingOptimizer.OptimizeAsync(request);

            if (!result.Success || result.Actions.Count == 0)
            {
                logger.LogDebug("AI optimization returned no results: {Error}", result.Error);
                return null;
            }

            // Convert optimizer actions back to DTOs (AI returns them in priority order)
            var optimizedSells = new List<TimedRebalancingActionDto>();
            var optimizedBuys = new List<TimedRebalancingActionDto>();
            var priorityIndex = 1;

            foreach (var action in result.Actions)
            {
                var candidate = rawCandidates.FirstOrDefault(c =>
                    string.Equals(c.Position.Ticker, action.Ticker, StringComparison.OrdinalIgnoreCase));

                if (candidate == null) continue;

                var dto = new TimedRebalancingActionDto
                {
                    ActionType = action.Type == "SELL" ? RebalancingActionType.Sell : RebalancingActionType.Buy,
                    Ticker = action.Ticker,
                    SecurityName = candidate.Position.SecurityName,
                    Amount = action.Amount,
                    Priority = priorityIndex++, // AI-assigned priority (1 = most important)
                    CurrentAllocationPercentage = Math.Round(candidate.Position.CurrentAllocationPercentage ?? 0, 2),
                    TargetAllocationPercentage = Math.Round(candidate.Position.TargetAllocationPercentage ?? 0, 2),
                    AllocationDeviation = Math.Round((candidate.Position.CurrentAllocationPercentage ?? 0) - (candidate.Position.TargetAllocationPercentage ?? 0), 2),
                    CurrentPrice = currentPrices.TryGetValue(action.Ticker, out var px) ? px : null,
                    TimingPercentile1Y = timingPercentiles.TryGetValue(action.Ticker, out var pctl) ? pctl : null,
                    UnrealizedPnLPercentage = candidate.Position.UnrealizedPnLPercentage,
                    Reason = action.Reason,
                    Confidence = action.Confidence
                };

                if (action.Type == "SELL")
                    optimizedSells.Add(dto);
                else
                    optimizedBuys.Add(dto);
            }

            return (optimizedSells, optimizedBuys, result.Summary);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI optimization failed, using deterministic results");
            return null;
        }
    }

    private static RebalancingOptimizerRequest BuildOptimizerRequest(
        List<TimedRebalancingActionDto> sells,
        List<TimedRebalancingActionDto> buys,
        List<RawCandidate> rawCandidates,
        Dictionary<string, decimal> timingPercentiles,
        Dictionary<string, decimal> currentPrices,
        decimal totalPortfolioValue,
        decimal cashAvailable,
        TimedRebalancingActionsOptions actionsOptions,
        int maxActions)
    {
        var securities = rawCandidates.Select(c => new RebalancingOptimizerSecurity
        {
            Ticker = c.Position.Ticker,
            SecurityName = c.Position.SecurityName,
            CurrentAllocation = c.Position.CurrentAllocationPercentage ?? 0,
            TargetAllocation = c.Position.TargetAllocationPercentage ?? 0,
            Deviation = (c.Position.CurrentAllocationPercentage ?? 0) - (c.Position.TargetAllocationPercentage ?? 0),
            GapValue = Math.Abs(c.SignedAmount),
            CurrentPrice = currentPrices.TryGetValue(c.Position.Ticker, out var px) ? px : null,
            Percentile1Y = timingPercentiles.TryGetValue(c.Position.Ticker, out var pctl) ? pctl : null,
            UnrealizedPnLPercent = c.Position.UnrealizedPnLPercentage,
            MarketValue = c.Position.CurrentMarketValue
        }).ToList();

        var sellCandidates = sells.Select(s => new RebalancingOptimizerCandidate
        {
            Ticker = s.Ticker,
            ActionType = "SELL",
            Amount = s.Amount,
            Confidence = s.Confidence,
            Reason = s.Reason
        }).ToList();

        var buyCandidates = buys.Select(b => new RebalancingOptimizerCandidate
        {
            Ticker = b.Ticker,
            ActionType = "BUY",
            Amount = b.Amount,
            Confidence = b.Confidence,
            Reason = b.Reason
        }).ToList();

        return new RebalancingOptimizerRequest
        {
            Constraints = new RebalancingOptimizerConstraints
            {
                NetCashflowTarget = 0,
                NoiseThreshold = actionsOptions.NoiseThreshold,
                MaxActions = maxActions,
                SellPercentileThreshold = actionsOptions.SellPercentileThreshold1Y,
                BuyPercentileThreshold = actionsOptions.BuyPercentileThreshold1Y,
                TotalPortfolioValue = totalPortfolioValue,
                CashAvailable = cashAvailable
            },
            Securities = securities,
            SellCandidates = sellCandidates,
            BuyCandidates = buyCandidates
        };
    }

    private static List<RawCandidate> BuildRawCandidates(
        IEnumerable<PortfolioPositionDto> positions,
        decimal totalPortfolioValue,
        decimal noiseThreshold)
    {
        var candidates = new List<RawCandidate>();

        foreach (var p in positions)
        {
            if (!p.CurrentAllocationPercentage.HasValue || !p.TargetAllocationPercentage.HasValue)
            {
                continue;
            }

            var current = p.CurrentAllocationPercentage.Value;
            var target = p.TargetAllocationPercentage.Value;
            var diffValue = (target - current) / 100m * totalPortfolioValue; // positive=buy, negative=sell

            if (Math.Abs(diffValue) < noiseThreshold)
            {
                continue;
            }

            candidates.Add(new RawCandidate(p, Math.Round(diffValue, 2)));
        }

        return candidates;
    }

    private async Task<Dictionary<string, decimal>> FetchTimingPercentiles1YAsync(
        List<string> tickers,
        Dictionary<string, decimal> currentPrices,
        List<PortfolioPositionDto> positions)
    {
        var percentiles = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        if (tickers.Count == 0)
        {
            return percentiles;
        }

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddYears(-1);

        foreach (var ticker in tickers)
        {
            var currentPrice = ResolveCurrentPrice(ticker, currentPrices, positions);
            if (currentPrice <= 0)
            {
                continue;
            }

            var history = await historicalPriceService.GetHistoricalPricesAsync(ticker, startDate, endDate);
            if (history.Count == 0)
            {
                continue;
            }

            var closes = history.Values.Where(v => v > 0).ToList();
            if (closes.Count == 0)
            {
                continue;
            }

            // Percentile definition: % of daily closes <= current price.
            var lessOrEqual = closes.Count(v => v <= currentPrice);
            var percentile = (decimal)lessOrEqual / closes.Count * 100m;

            percentiles[ticker] = Math.Round(percentile, 2);
        }

        return percentiles;
    }

    private static decimal ResolveCurrentPrice(
        string ticker,
        Dictionary<string, decimal> currentPrices,
        List<PortfolioPositionDto> positions)
    {
        if (currentPrices.TryGetValue(ticker, out var price) && price > 0)
        {
            return price;
        }

        var position = positions.FirstOrDefault(p => string.Equals(p.Ticker, ticker, StringComparison.OrdinalIgnoreCase));
        if (position?.CurrentMarketValue is > 0 && position.TotalShares > 0)
        {
            return position.CurrentMarketValue.Value / position.TotalShares;
        }

        return 0;
    }

    private static (List<TimedRebalancingActionDto> Sells, List<TimedRebalancingActionDto> Buys) ApplyTimingFilterAndBuildActions(
        List<RawCandidate> candidates,
        Dictionary<string, decimal> timingPercentiles,
        Dictionary<string, decimal> currentPrices,
        TimedRebalancingActionsOptions options,
        int maxActions)
    {
        var sells = new List<TimedRebalancingActionDto>();
        var buys = new List<TimedRebalancingActionDto>();

        var sellCandidates = candidates
            .Where(c => c.SignedAmount < 0)
            .OrderByDescending(c => Math.Abs(c.SignedAmount))
            .ToList();

        var buyCandidates = candidates
            .Where(c => c.SignedAmount > 0)
            .OrderByDescending(c => Math.Abs(c.SignedAmount))
            .ToList();

        var timingFilteredSells = sellCandidates
            .Where(c => timingPercentiles.TryGetValue(c.Position.Ticker, out var pctl) && pctl >= options.SellPercentileThreshold1Y)
            .ToList();

        var timingFilteredBuys = buyCandidates
            .Where(c => timingPercentiles.TryGetValue(c.Position.Ticker, out var pctl) && pctl <= options.BuyPercentileThreshold1Y)
            .ToList();

        // Fallback: if filtering removes everything, include top gaps with low confidence.
        if (timingFilteredSells.Count == 0)
        {
            timingFilteredSells = sellCandidates.Take(maxActions).ToList();
        }

        if (timingFilteredBuys.Count == 0)
        {
            timingFilteredBuys = buyCandidates.Take(maxActions).ToList();
        }

        foreach (var c in timingFilteredSells.Take(maxActions))
        {
            sells.Add(BuildAction(c, RebalancingActionType.Sell, timingPercentiles, currentPrices, options));
        }

        foreach (var c in timingFilteredBuys.Take(maxActions))
        {
            buys.Add(BuildAction(c, RebalancingActionType.Buy, timingPercentiles, currentPrices, options));
        }

        return (sells, buys);
    }

    /// <summary>
    /// Validates and adjusts actions based on available funds.
    /// Supports three scenarios:
    /// 1. Sell only → User accumulates cash (no good buys available)
    /// 2. Buy only → User deploys existing cash (no good sells available)
    /// 3. Sell → Buy → Classic rebalancing
    ///
    /// Does NOT force pairing. Sells are always valid if timing is good.
    /// Buys are capped to available funds (cash + sell proceeds).
    /// </summary>
    private static (List<TimedRebalancingActionDto> Sells, List<TimedRebalancingActionDto> Buys) ApplyPairing(
        List<TimedRebalancingActionDto> sells,
        List<TimedRebalancingActionDto> buys,
        decimal cashAvailable,
        decimal noiseThreshold)
    {
        if (sells.Count == 0 && buys.Count == 0)
        {
            return (sells, buys);
        }

        // Sells are always valid - user can choose to sell overweight/expensive positions
        // regardless of whether they want to buy anything
        var validSells = sells;

        // Buys are limited by available funds
        var totalSellProceeds = validSells.Sum(s => s.Amount);
        var availableFunds = totalSellProceeds + cashAvailable;

        if (buys.Count == 0 || availableFunds <= 0)
        {
            // Sell-only scenario (or no funds for buys)
            return (validSells, []);
        }

        var totalBuyDemand = buys.Sum(b => b.Amount);

        // If we have enough funds for all buys, return as-is
        if (availableFunds >= totalBuyDemand)
        {
            return (validSells, buys);
        }

        // Not enough funds - scale buys proportionally to fit available funds
        var buyScaleFactor = availableFunds / totalBuyDemand;
        var scaledBuys = buys
            .Select(b => ScaleAction(b, buyScaleFactor, noiseThreshold))
            .Where(b => b != null)
            .Cast<TimedRebalancingActionDto>()
            .ToList();

        return (validSells, scaledBuys);
    }

    /// <summary>
    /// Scales an action's amount by a factor. Returns null if result is below noise threshold.
    /// </summary>
    private static TimedRebalancingActionDto? ScaleAction(TimedRebalancingActionDto action, decimal scaleFactor, decimal noiseThreshold)
    {
        var newAmount = Math.Round(action.Amount * scaleFactor, 2);

        if (newAmount < noiseThreshold)
        {
            return null;
        }

        return new TimedRebalancingActionDto
        {
            ActionType = action.ActionType,
            Ticker = action.Ticker,
            SecurityName = action.SecurityName,
            Amount = newAmount,
            CurrentAllocationPercentage = action.CurrentAllocationPercentage,
            TargetAllocationPercentage = action.TargetAllocationPercentage,
            AllocationDeviation = action.AllocationDeviation,
            CurrentPrice = action.CurrentPrice,
            TimingPercentile1Y = action.TimingPercentile1Y,
            UnrealizedPnLPercentage = action.UnrealizedPnLPercentage,
            Reason = action.Reason,
            Confidence = action.Confidence
        };
    }

    private static TimedRebalancingActionDto BuildAction(
        RawCandidate candidate,
        RebalancingActionType actionType,
        Dictionary<string, decimal> timingPercentiles,
        Dictionary<string, decimal> currentPrices,
        TimedRebalancingActionsOptions options)
    {
        var p = candidate.Position;
        var ticker = p.Ticker;

        timingPercentiles.TryGetValue(ticker, out var percentile);
        var hasTiming = timingPercentiles.ContainsKey(ticker);

        var currentPrice = currentPrices.TryGetValue(ticker, out var px) ? px : (decimal?)null;

        var amount = Math.Abs(candidate.SignedAmount);
        var deviation = (p.CurrentAllocationPercentage ?? 0) - (p.TargetAllocationPercentage ?? 0);

        // TODO: Improve reason, maybe consider introducing gemini at this point? Improvement opportunity.∫
        var reason = actionType == RebalancingActionType.Sell
            ? $"Overweight vs target and {(hasTiming ? $"expensive vs 1Y history (pctl={percentile:F0})" : "timing unavailable")}."
            : $"Underweight vs target and {(hasTiming ? $"cheap vs 1Y history (pctl={percentile:F0})" : "timing unavailable")}.";

        var confidence = hasTiming
            ? actionType == RebalancingActionType.Sell
                ? ConfidenceFromSellPercentile(percentile, options.SellPercentileThreshold1Y)
                : ConfidenceFromBuyPercentile(percentile, options.BuyPercentileThreshold1Y)
            : 0.2m;

        return new TimedRebalancingActionDto
        {
            ActionType = actionType,
            Ticker = ticker,
            SecurityName = p.SecurityName,
            Amount = Math.Round(amount, 2),
            CurrentAllocationPercentage = Math.Round(p.CurrentAllocationPercentage ?? 0, 2),
            TargetAllocationPercentage = Math.Round(p.TargetAllocationPercentage ?? 0, 2),
            AllocationDeviation = Math.Round(deviation, 2),
            CurrentPrice = currentPrice,
            TimingPercentile1Y = hasTiming ? percentile : null,
            UnrealizedPnLPercentage = p.UnrealizedPnLPercentage,
            Reason = reason,
            Confidence = Math.Round(confidence, 2)
        };
    }

    private static decimal ConfidenceFromSellPercentile(decimal percentile, decimal threshold)
    {
        if (percentile < threshold)
        {
            return 0.3m;
        }

        var denom = 100m - threshold;
        if (denom <= 0)
        {
            return 0.8m;
        }

        return Math.Clamp((percentile - threshold) / denom, 0.3m, 0.95m);
    }

    private static decimal ConfidenceFromBuyPercentile(decimal percentile, decimal threshold)
    {
        if (percentile > threshold)
        {
            return 0.3m;
        }

        if (threshold <= 0)
        {
            return 0.8m;
        }

        return Math.Clamp((threshold - percentile) / threshold, 0.3m, 0.95m);
    }

    private sealed record RawCandidate(PortfolioPositionDto Position, decimal SignedAmount);
}
