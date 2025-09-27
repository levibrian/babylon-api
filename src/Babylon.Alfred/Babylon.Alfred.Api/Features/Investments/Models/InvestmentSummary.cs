namespace Babylon.Alfred.Api.Features.Investments.Models;

public class InvestmentSummary
{
    public decimal TotalInvested { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalValue { get; set; } // Current value (would need real-time prices)
    public decimal TotalGainLoss { get; set; } // TotalValue - TotalInvested
    public decimal TotalGainLossPercentage { get; set; } // (TotalGainLoss / TotalInvested) * 100
    public int TotalTransactions { get; set; }
    public int UniqueAssets { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public List<AssetHolding> TopHoldings { get; set; } = new();
    public List<AssetTypeSummary> AssetTypeBreakdown { get; set; } = new();
}

public class AssetHolding
{
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public decimal TotalShares { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; } // Would need real-time prices
    public decimal GainLoss { get; set; }
    public decimal GainLossPercentage { get; set; }
}

public class AssetTypeSummary
{
    public AssetType AssetType { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal Percentage { get; set; }
    public int AssetCount { get; set; }
}

