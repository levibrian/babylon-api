namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

/// <summary>
/// Request for smart rebalancing recommendations.
/// </summary>
public class SmartRebalancingRequestDto
{
    /// <summary>
    /// Amount to invest (in currency).
    /// </summary>
    public decimal InvestmentAmount { get; set; }

    /// <summary>
    /// Maximum number of securities to include in recommendations (null = all underweight).
    /// </summary>
    public int? MaxSecurities { get; set; }

    /// <summary>
    /// If true, only recommend buying underweight positions (no sells).
    /// </summary>
    public bool OnlyBuyUnderweight { get; set; } = true;
}

