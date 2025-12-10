namespace Babylon.Alfred.Api.Features.Investments.Models.Responses.Rebalancing;

/// <summary>
/// Response containing smart rebalancing recommendations based on proportional gap distribution.
/// </summary>
public class SmartRebalancingResponseDto
{
    /// <summary>
    /// Returns an empty response for portfolios with no recommendations.
    /// </summary>
    public static SmartRebalancingResponseDto Empty => new()
    {
        TotalInvestmentAmount = 0,
        SecuritiesCount = 0,
        Recommendations = new List<SmartRebalancingRecommendationDto>()
    };

    /// <summary>
    /// List of recommended buy actions for underweight positions.
    /// </summary>
    public List<SmartRebalancingRecommendationDto> Recommendations { get; init; } = new();

    /// <summary>
    /// Total investment amount allocated.
    /// </summary>
    public decimal TotalInvestmentAmount { get; init; }

    /// <summary>
    /// Number of securities included in recommendations.
    /// </summary>
    public int SecuritiesCount { get; init; }
}

/// <summary>
/// A single smart rebalancing recommendation.
/// </summary>
public class SmartRebalancingRecommendationDto
{
    /// <summary>
    /// Ticker symbol of the security.
    /// </summary>
    public string Ticker { get; init; } = string.Empty;

    /// <summary>
    /// Security name.
    /// </summary>
    public string SecurityName { get; init; } = string.Empty;

    /// <summary>
    /// Current allocation percentage.
    /// </summary>
    public decimal CurrentAllocationPercentage { get; init; }

    /// <summary>
    /// Target allocation percentage.
    /// </summary>
    public decimal TargetAllocationPercentage { get; init; }

    /// <summary>
    /// Gap score (Target% - Current%).
    /// </summary>
    public decimal GapScore { get; init; }

    /// <summary>
    /// Recommended buy amount for this security.
    /// </summary>
    public decimal RecommendedBuyAmount { get; init; }
}

