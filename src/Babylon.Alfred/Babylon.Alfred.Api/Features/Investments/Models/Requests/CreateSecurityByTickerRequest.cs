using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

/// <summary>
/// Request to create a security by ticker symbol.
/// The system will automatically fetch metadata from Yahoo Finance.
/// </summary>
public class CreateSecurityByTickerRequest
{
    [Required]
    [MaxLength(50)]
    public string Ticker { get; set; } = string.Empty;
}
