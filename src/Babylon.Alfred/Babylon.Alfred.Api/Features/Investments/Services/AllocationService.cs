using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class AllocationService(
    IAllocationStrategyRepository allocationStrategyRepository,
    ISecurityRepository securityRepository) : IAllocationService
{
    public async Task<IList<AllocationTargetDto>> GetTargets(Guid userId)
    {
        var strategies = await allocationStrategyRepository.GetByUserIdAsync(userId);
        return ToDtos(strategies.Select(s => (s.Security.Ticker, s.TargetPercentage)));
    }

    public async Task<IList<AllocationTargetDto>> ReplaceTargets(Guid userId, UpdateAllocationTargetsRequest request)
    {
        var targets = (request.Targets ?? new List<AllocationTargetRequest>())
            .Select(t => (Ticker: (t.Ticker ?? string.Empty).Trim().ToUpperInvariant(), t.TargetPercentage))
            .ToList();

        var duplicate = targets.GroupBy(t => t.Ticker).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new ArgumentException($"Ticker '{duplicate.Key}' appears more than once");
        }

        var outOfRange = targets.Where(t => t.TargetPercentage < 0 || t.TargetPercentage > 100).Select(t => t.Ticker).FirstOrDefault();
        if (outOfRange != null)
        {
            throw new ArgumentException($"Target for '{outOfRange}' must be between 0 and 100");
        }

        var total = targets.Sum(t => t.TargetPercentage);
        if (total > 100)
        {
            throw new ArgumentException($"Targets add up to {total}%, which exceeds 100%");
        }

        var securities = await securityRepository.GetByTickersAsync(targets.Select(t => t.Ticker));
        var unknown = targets.Where(t => !securities.ContainsKey(t.Ticker)).Select(t => t.Ticker).ToList();
        if (unknown.Count > 0)
        {
            throw new ArgumentException($"Unknown ticker(s): {string.Join(", ", unknown)}");
        }

        var now = DateTime.UtcNow;
        var strategies = targets
            .Select(t => new AllocationStrategy
            {
                UserId = userId,
                SecurityId = securities[t.Ticker].Id,
                TargetPercentage = t.TargetPercentage,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        await allocationStrategyRepository.ReplaceForUserAsync(userId, strategies);
        return ToDtos(targets);
    }

    private static IList<AllocationTargetDto> ToDtos(IEnumerable<(string Ticker, decimal TargetPercentage)> targets)
    {
        return targets
            .OrderByDescending(t => t.TargetPercentage)
            .ThenBy(t => t.Ticker)
            .Select(t => new AllocationTargetDto { Ticker = t.Ticker, TargetPercentage = t.TargetPercentage })
            .ToList();
    }
}
