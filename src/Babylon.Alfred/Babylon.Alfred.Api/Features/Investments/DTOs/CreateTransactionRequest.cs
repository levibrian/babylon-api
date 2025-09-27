using System.ComponentModel.DataAnnotations;
using Babylon.Alfred.Api.Features.Investments.Models;

namespace Babylon.Alfred.Api.Features.Investments.DTOs;

public class CreateTransactionRequest
{
    [Required]
    [MaxLength(50)]
    public string AssetSymbol { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AssetName { get; set; } = string.Empty;
    
    [Required]
    public AssetType AssetType { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "Shares quantity must be greater than 0")]
    public decimal SharesQuantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Share price must be greater than 0")]
    public decimal SharePrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Fees cannot be negative")]
    public decimal Fees { get; set; } = 0;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

