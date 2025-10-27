namespace Babylon.Alfred.Api.Shared.Data.Models;

public class Company
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
}

