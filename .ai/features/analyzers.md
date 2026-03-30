# Portfolio Analyzers — Strategy Pattern

## Overview

Implements the **Strategy Pattern** for portfolio analysis. Each analyzer examines a different aspect of portfolio health and generates actionable insights. All implement `IPortfolioAnalyzer`.

---

## Interface Contract

```csharp
public interface IPortfolioAnalyzer
{
    Task<List<PortfolioInsight>> AnalyzeAsync(
        PortfolioResponse portfolio,
        List<PortfolioSnapshotDto> historicalSnapshots
    );
}
```

**Output**: `PortfolioInsight` with `Category`, `Severity` (Info/Warning/Critical), `Message`, `RecommendedAction`.

---

## Analyzer Inventory

| Analyzer | Category | Focus |
|----------|----------|-------|
| `RiskAnalyzer` | Risk | Concentration risk, diversification (HHI), asset count |
| `IncomeAnalyzer` | Income | Dividend yield patterns, income stream consistency |
| `EfficiencyAnalyzer` | Efficiency | Sharpe ratio, return/risk ratios, S&P 500 benchmark comparison |
| `TrendAnalyzer` | Trend | Price momentum, drawdown alerts, portfolio growth trajectory |

---

## Threshold Boundaries (Must Be Tested)

### RiskAnalyzer
- Concentration **>20%** → Warning severity
- Concentration **>40%** → Critical severity
- HHI (Herfindahl-Hirschman Index) for diversification score

### EfficiencyAnalyzer Constants
- Risk-free rate: **3%** (US Treasury approximation)
- Benchmark: **S&P 500 (`^GSPC`)**
- Trading days/year: **252**

### Rebalancing Thresholds (via InvestmentService, not analyzers)
- Buy percentile: **<20th** of 1-year price range
- Sell percentile: **>80th** of 1-year price range

---

## DI Registration

All registered in `Features/Investments/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IPortfolioAnalyzer, RiskAnalyzer>();
services.AddScoped<IPortfolioAnalyzer, IncomeAnalyzer>();
services.AddScoped<IPortfolioAnalyzer, EfficiencyAnalyzer>();
services.AddScoped<IPortfolioAnalyzer, TrendAnalyzer>();
```

Injected as `IEnumerable<IPortfolioAnalyzer>` into `PortfolioInsightsService` for automatic discovery.

---

## Testing Requirements

- **Pure input → output tests (no mocks)**
- Test thresholds **at, below, and above** (e.g., 19.9%, 20.0%, 20.1%)
- Test edge cases: empty portfolio, single position, fully diversified

### Example Test Pattern

```csharp
[Theory]
[InlineData(0.199, Severity.Info)]     // Below threshold
[InlineData(0.20, Severity.Warning)]   // At threshold
[InlineData(0.41, Severity.Critical)]  // Above critical
public async Task RiskAnalyzer_ConcentrationThresholds_CorrectSeverity(
    decimal concentration, Severity expectedSeverity)
{
    var portfolio = CreatePortfolioWithConcentration(concentration);
    var analyzer = new RiskAnalyzer();
    var insights = await analyzer.AnalyzeAsync(portfolio, new List<PortfolioSnapshotDto>());
    var insight = insights.First(i => i.Message.Contains("concentration"));
    insight.Severity.Should().Be(expectedSeverity);
}
```

---

## Invariants

- **Analyzers must be stateless** (no instance fields except injected dependencies)
- **Insights must be actionable** (tell user what to do, not just observations)
- **Historical data may be empty** — analyzers must handle gracefully

---

## Adding a New Analyzer

1. Create class implementing `IPortfolioAnalyzer`
2. Add to DI registration (above)
3. Define thresholds as constants at top of class
4. Add threshold documentation to this file
5. Write boundary tests for all thresholds
6. Update `PortfolioInsight.Category` enum if needed
