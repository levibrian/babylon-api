using Babylon.Alfred.Api.Features.Investments.Models;

namespace Babylon.Alfred.Api.Features.Investments.DTOs;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal SharesQuantity { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Fees { get; set; }
    public decimal TotalAmountInvested { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

