namespace Babylon.Alfred.Api.Features.Investments.Options;

/// <summary>
/// Options for Gemini-powered rebalancing optimization.
/// </summary>
public class GeminiRebalancingOptions
{
    public const string SectionName = "Rebalancing:Gemini";

    /// <summary>Whether Gemini optimization is enabled.</summary>
    public bool Enabled { get; init; } = false;

    /// <summary>Gemini API key.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Gemini model to use (e.g., "gemini-1.5-flash").</summary>
    public string Model { get; init; } = "gemini-1.5-flash";

    /// <summary>API endpoint base URL.</summary>
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Temperature for generation (0.0 - 1.0).</summary>
    public double Temperature { get; init; } = 0.3;
}
