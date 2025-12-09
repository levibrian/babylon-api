namespace Babylon.Alfred.Api.Features.Investments.Models.Responses;

/// <summary>
/// DTO for market search results from Yahoo Finance API.
/// </summary>
public class SearchResultDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
}
