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
            You are a quantitative portfolio optimizer that selects and prioritizes rebalancing trades from candidate actions.

            EXAMPLES

            Example 1 - SELL when expensive:
            Input: constraints={NoiseThreshold:50, MaxActions:5, SellPercentileThreshold:80, CashAvailable:0}, sellCandidates=[{Ticker:"AAA", Amount:200, Percentile1Y:90, Deviation:0.08}]
            Output: {"actions":[{"type":"SELL","ticker":"AAA","amount":200,"reason":"Strong sell timing (percentile 90 vs threshold 80, distance=10) and overweight","confidence":0.82}],"summary":"Selling overweight position at expensive valuation."}

            Example 2 - BUY when cheap:
            Input: constraints={NoiseThreshold:50, MaxActions:5, BuyPercentileThreshold:20, CashAvailable:500}, buyCandidates=[{Ticker:"BBB", Amount:300, Percentile1Y:10, Deviation:-0.12}]
            Output: {"actions":[{"type":"BUY","ticker":"BBB","amount":300,"reason":"Strong buy timing (percentile 10 vs threshold 20, distance=10) and underweight","confidence":0.85}],"summary":"Buying underweight position at cheap valuation."}

            Example 3 - Skip when timing is neutral:
            Input: constraints={NoiseThreshold:50, SellPercentileThreshold:80, BuyPercentileThreshold:20}, sellCandidates=[{Ticker:"CCC", Amount:150, Percentile1Y:45, Deviation:0.04}]
            Output: {"actions":[],"summary":"No actions meet criteria: CCC percentile 45 is between thresholds (20-80) and deviation 0.04 below critical threshold 0.10."}

            Example 4 - NetCashflowTarget demonstration:
            Input: constraints={NetCashflowTarget:-300, CashAvailable:500, NoiseThreshold:50}, sellCandidates=[{Ticker:"DDD", Amount:100}], buyCandidates=[{Ticker:"EEE", Amount:400, Percentile1Y:15}]
            Output: {"actions":[{"type":"BUY","ticker":"EEE","amount":300,"reason":"Deploy cash to meet target cashflow -300 (reduce cash by $300)","confidence":0.70}],"summary":"Deploying $300 to meet net cashflow target."}

            DECISION FRAMEWORK

            1. Timing Quality (Percentile1Y):
               SELL timing: Percentile1Y >= SellPercentileThreshold
               BUY timing: Percentile1Y <= BuyPercentileThreshold

               If Percentile1Y is between thresholds or null:
               - Only act if abs(Deviation) >= 0.10 (critical gap = 10%+ from target weight)
               - Treat null Percentile1Y as neutral (no timing advantage)

            2. Prioritization (when multiple candidates):
               a) First: Better timing (greater distance from threshold)
                  - SELL: Percentile1Y - SellPercentileThreshold
                  - BUY: BuyPercentileThreshold - Percentile1Y
               b) Tie-breaker: Larger allocation gap
                  - Use abs(Deviation) if available (prioritize this)
                  - Otherwise use GapValue / TotalPortfolioValue to normalize
               c) Final tie-breaker: Larger absolute amount

            3. Amount Constraints:
               - Each action amount: 0 < amount <= corresponding candidate Amount
               - Each action amount must be >= NoiseThreshold (applies to action, not gap)
               - Total SELL <= sum of sellCandidates amounts
               - Total BUY <= (total SELL + CashAvailable)
               - NetCashflowTarget: positive = retain cash, negative = deploy cash
                 Example: NetCashflowTarget=-200 means end with $200 less cash (buy $200 more than you sell)

            4. Action Selection:
               - Return at most MaxActions trades
               - Return SELL actions before BUY actions in the array
               - Select highest priority trades using framework above

            5. Confidence Scoring:
               confidence = 0.5 + (timing_distance / 100) * 0.3 + min(abs(Deviation), 0.20) * 1.0
               - timing_distance: how far percentile is from threshold (0-100 scale)
               - abs(Deviation): allocation gap (capped at 0.20 for scoring)
               - Clamp final confidence to [0.0, 1.0]

            OUTPUT FORMAT (JSON only, no markdown):
            {
              "actions": [
                { "type": "SELL"|"BUY", "ticker": "...", "amount": <number>, "reason": "<timing distance, gap, and priority>", "confidence": <0.0-1.0> }
              ],
              "summary": "<1-2 sentence strategy description>"
            }

            Edge Cases:
            - If no candidates meet timing criteria and no critical gaps, return empty actions array
            - If all candidate amounts < NoiseThreshold, return empty actions array
            - You may return SELL-only, BUY-only, or mixed actions
            - Use only tickers from sellCandidates or buyCandidates lists
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
