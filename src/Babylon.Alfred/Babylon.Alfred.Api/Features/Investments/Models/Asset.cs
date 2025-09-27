using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.Investments.Models;

public class Asset
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Symbol { get; set; } = string.Empty; // e.g., "NVDA", "AAPL"
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty; // e.g., "Nvidia", "Apple"
    
    [Required]
    public AssetType Type { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "EUR"; // Default to EUR based on your data
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Constructor
    public Asset()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

