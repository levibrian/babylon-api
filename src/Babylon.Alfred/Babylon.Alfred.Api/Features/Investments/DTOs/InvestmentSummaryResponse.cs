using Babylon.Alfred.Api.Features.Investments.Models;

namespace Babylon.Alfred.Api.Features.Investments.DTOs;

public class InvestmentSummaryResponse
{
    public decimal TotalInvested { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalGainLoss { get; set; }
    public decimal TotalGainLossPercentage { get; set; }
    public int TotalTransactions { get; set; }
    public int UniqueAssets { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public List<AssetHoldingResponse> TopHoldings { get; set; } = new();
    public List<AssetTypeSummaryResponse> AssetTypeBreakdown { get; set; } = new();
}

public class AssetHoldingResponse
{
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public decimal TotalShares { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal GainLoss { get; set; }
    public decimal GainLossPercentage { get; set; }
}

public class AssetTypeSummaryResponse
{
    public AssetType AssetType { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal Percentage { get; set; }
    public int AssetCount { get; set; }
}

