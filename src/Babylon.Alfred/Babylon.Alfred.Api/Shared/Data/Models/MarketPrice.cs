namespace Babylon.Alfred.Api.Shared.Data.Models;

/// <summary>
/// Stores cached market prices for securities.
/// Updated periodically by the background worker from Yahoo Finance.
/// </summary>
public class MarketPrice
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign key to the Security entity.
    /// Normalized for referential integrity and consistency with Transaction/AllocationStrategy.
    /// </summary>
    public Guid SecurityId { get; set; }
    
    /// <summary>
    /// The current/last known price for this security.
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Currency of the price (e.g., "USD", "EUR").
    /// </summary>
    public string? Currency { get; set; }
    
    /// <summary>
    /// When this price was last updated from the data source.
    /// </summary>
    public DateTime LastUpdated { get; set; }
    
    // Navigation property
    public Security Security { get; set; } = null!;
}

