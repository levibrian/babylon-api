using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.Investments.Models;

public class Transaction
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AssetSymbol { get; set; } = string.Empty; // Company/Asset identifier (e.g., "NVDA", "AAPL")
    
    [Required]
    [MaxLength(200)]
    public string AssetName { get; set; } = string.Empty; // Company name (e.g., "Nvidia", "Apple")
    
    [Required]
    public AssetType AssetType { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; } // Amount invested (e.g., €25.00)
    
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "Shares quantity must be greater than 0")]
    public decimal SharesQuantity { get; set; } // Quantity of shares (e.g., 0.271237)
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Share price must be greater than 0")]
    public decimal SharePrice { get; set; } // Price per share (e.g., €92.48)
    
    [Range(0, double.MaxValue, ErrorMessage = "Fees cannot be negative")]
    public decimal Fees { get; set; } // Fees incurred (e.g., €0, €1)
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount invested must be greater than 0")]
    public decimal TotalAmountInvested { get; set; } // Amount + Fees (e.g., €26.00)
    
    [MaxLength(500)]
    public string? Notes { get; set; } // Optional additional information
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Constructor
    public Transaction()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

