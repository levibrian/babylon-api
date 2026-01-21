using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Babylon.Alfred.Api.Features.Investments.Options;
using Microsoft.Extensions.Options;

namespace Babylon.Alfred.Api.Features.Investments.Services;

/// <summary>
/// Gemini-powered rebalancing optimizer.
/// Feature-flagged: only active when Rebalancing:Gemini:Enabled = true.
/// </summary>
public class GeminiRebalancingOptimizer : IRebalancingOptimizer
{
    private readonly HttpClient _httpClient;
    private readonly GeminiRebalancingOptions _options;
    private readonly ILogger<GeminiRebalancingOptimizer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public GeminiRebalancingOptimizer(
        HttpClient httpClient,
        IOptions<GeminiRebalancingOptions> options,
        ILogger<GeminiRebalancingOptimizer> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<RebalancingOptimizerResponse> OptimizeAsync(RebalancingOptimizerRequest request)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Gemini rebalancing optimizer is disabled");
            return new RebalancingOptimizerResponse
            {
                Success = false,
                Error = "Gemini optimization is disabled"
            };
        }

        try
        {
            var prompt = BuildPrompt(request);
            var geminiResponse = await CallGeminiAsync(prompt);

            if (geminiResponse == null)
            {
                return new RebalancingOptimizerResponse
                {
                    Success = false,
                    Error = "Failed to get response from Gemini"
                };
            }

            var validated = ValidateAndParseResponse(geminiResponse, request);
            return validated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini for rebalancing optimization");
            return new RebalancingOptimizerResponse
            {
                Success = false,
                Error = $"Optimization failed: {ex.Message}"
            };
        }
    }

    private static string BuildPrompt(RebalancingOptimizerRequest request)
    {
        var systemInstruction = """
            You are a quantitative portfolio optimizer. Your job is to CRITICALLY ANALYZE and OPTIMIZE a set of proposed rebalancing actions.

            ## YOUR TASK
            You receive pre-computed sell/buy candidates from a deterministic algorithm. Your job is to:
            1. **FILTER** - Remove actions that don't make sense
            2. **PRIORITIZE** - Reorder by impact: which actions matter most for portfolio health?
            3. **ADJUST AMOUNTS** - Optimize amounts based on the full picture
            4. **VALIDATE** - Does each action make sense given the others?

            ## THREE VALID SCENARIOS
            You can return ANY of these combinations:
            1. **SELL only** - Overweight positions with good timing, user accumulates cash. Valid when no good buys exist.
            2. **BUY only** - Underweight positions with good timing, funded by existing cash. Valid when no good sells exist.
            3. **SELL → BUY** - Classic rebalancing: sell overweight/expensive to buy underweight/cheap.

            Do NOT force pairing. If only sells make sense, return only sells. If only buys make sense (and cash is available), return only buys.

            ## DECISION CRITERIA (in order of importance)
            1. **Timing quality** - Percentile1Y indicates if price is historically cheap (<=20) or expensive (>=80)
            2. **Allocation gap severity** - Larger deviations from target are more urgent
            3. **P&L context** - Consider unrealized gains/losses (profit-taking opportunities)
            4. **Efficiency** - Don't trade for marginal improvements (<5% deviation with neutral timing)

            ## WHAT TO FILTER OUT
            - Actions with <$50 impact (noise)
            - Buys where timing is poor (percentile1Y > 50) unless severely underweight (>10% gap)
            - Sells where timing is poor (percentile1Y < 50) unless severely overweight (>10% gap)
            - Keep sells even without corresponding buys if timing is excellent (>85 percentile)
            - Keep buys even without corresponding sells if timing is excellent (<15 percentile) AND cash is available

            ## CONSTRAINTS (MUST RESPECT)
            - Only use tickers from the provided list
            - Total sell amount ≤ sum of sell candidates
            - Total buy amount ≤ sells + cashAvailable
            - Return actions ordered by priority (most important first)

            ## OUTPUT FORMAT
            Return ONLY valid JSON (no markdown):
            {
              "actions": [
                { "type": "SELL" | "BUY", "ticker": "...", "amount": number, "reason": "why this action, why this priority", "confidence": 0.0-1.0 }
              ],
              "summary": "1-2 sentences: what's the strategy (sell-only to accumulate cash / buy-only to deploy cash / rebalance)"
            }

            If none of the candidates make sense, return empty actions array with explanation in summary.
            """;

        var inputJson = JsonSerializer.Serialize(new
        {
            constraints = request.Constraints,
            securities = request.Securities,
            sellCandidates = request.SellCandidates,
            buyCandidates = request.BuyCandidates
        }, JsonOptions);

        return $"{systemInstruction}\n\nINPUT:\n{inputJson}";
    }

    private async Task<string?> CallGeminiAsync(string prompt)
    {
        var url = $"{_options.BaseUrl}/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _options.Temperature,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Calling Gemini API for rebalancing optimization");

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Gemini API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        // Parse Gemini response structure to extract the generated text
        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text;
    }

    private RebalancingOptimizerResponse ValidateAndParseResponse(
        string geminiResponse,
        RebalancingOptimizerRequest request)
    {
        try
        {
            // Parse the JSON response
            using var doc = JsonDocument.Parse(geminiResponse);
            var root = doc.RootElement;

            var actions = new List<RebalancingOptimizerAction>();
            var validTickers = request.Securities.Select(s => s.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("actions", out var actionsElement))
            {
                foreach (var actionEl in actionsElement.EnumerateArray())
                {
                    var ticker = actionEl.GetProperty("ticker").GetString() ?? "";
                    var type = actionEl.GetProperty("type").GetString() ?? "";
                    var amount = actionEl.GetProperty("amount").GetDecimal();
                    var reason = actionEl.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
                    var confidence = actionEl.TryGetProperty("confidence", out var c) ? c.GetDecimal() : 0.5m;

                    // Validate ticker exists
                    if (!validTickers.Contains(ticker))
                    {
                        _logger.LogWarning("Gemini returned unknown ticker {Ticker}, skipping", ticker);
                        continue;
                    }

                    // Validate action type
                    if (type != "SELL" && type != "BUY")
                    {
                        _logger.LogWarning("Gemini returned invalid action type {Type}, skipping", type);
                        continue;
                    }

                    // Validate amount is positive
                    if (amount <= 0)
                    {
                        _logger.LogWarning("Gemini returned non-positive amount {Amount} for {Ticker}, skipping", amount, ticker);
                        continue;
                    }

                    actions.Add(new RebalancingOptimizerAction
                    {
                        Type = type,
                        Ticker = ticker,
                        Amount = Math.Round(amount, 2),
                        Reason = reason,
                        Confidence = Math.Clamp(confidence, 0m, 1m)
                    });
                }
            }

            // Validate constraints
            var totalSell = actions.Where(a => a.Type == "SELL").Sum(a => a.Amount);
            var totalBuy = actions.Where(a => a.Type == "BUY").Sum(a => a.Amount);
            var maxSell = request.SellCandidates.Sum(c => c.Amount);
            var maxBuy = totalSell + request.Constraints.CashAvailable;

            // Trim if exceeds limits
            if (totalSell > maxSell)
            {
                var scale = maxSell / totalSell;
                actions = actions.Select(a => a.Type == "SELL"
                    ? new RebalancingOptimizerAction
                    {
                        Type = a.Type,
                        Ticker = a.Ticker,
                        Amount = Math.Round(a.Amount * scale, 2),
                        Reason = a.Reason,
                        Confidence = a.Confidence
                    }
                    : a).ToList();
            }

            if (totalBuy > maxBuy)
            {
                var scale = maxBuy / totalBuy;
                actions = actions.Select(a => a.Type == "BUY"
                    ? new RebalancingOptimizerAction
                    {
                        Type = a.Type,
                        Ticker = a.Ticker,
                        Amount = Math.Round(a.Amount * scale, 2),
                        Reason = a.Reason,
                        Confidence = a.Confidence
                    }
                    : a).ToList();
            }

            var summary = root.TryGetProperty("summary", out var summaryEl) ? summaryEl.GetString() : null;

            return new RebalancingOptimizerResponse
            {
                Success = true,
                Actions = actions,
                Summary = summary
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini response as JSON");
            return new RebalancingOptimizerResponse
            {
                Success = false,
                Error = "Failed to parse AI response"
            };
        }
    }
}
