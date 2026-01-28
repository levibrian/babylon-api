using System.Text.Json.Serialization;

namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Models;

public class YahooSearchResponse
{
    [JsonPropertyName("quotes")]
    public List<YahooSearchResult> Quotes { get; set; } = [];
}

public class YahooSearchResult
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("shortname")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("longname")]
    public string LongName { get; set; } = string.Empty;

    [JsonPropertyName("quoteType")]
    public string QuoteType { get; set; } = string.Empty;

    [JsonPropertyName("exchange")]
    public string Exchange { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("sector")]
    public string? Sector { get; set; }

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    /// <summary>
    /// ISIN (International Securities Identification Number).
    /// Note: Yahoo Finance search API does not provide this field.
    /// Will be null unless populated from another source.
    /// </summary>
    [JsonPropertyName("isin")]
    public string? Isin { get; set; }
}
