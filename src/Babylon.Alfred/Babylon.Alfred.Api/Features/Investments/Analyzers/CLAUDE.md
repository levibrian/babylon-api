# Investments Analyzers - Portfolio Analysis Strategy Pattern

## Overview

Implements the **Strategy Pattern** for portfolio analysis. Each analyzer examines different aspects of portfolio health and generates actionable insights. All analyzers implement `IPortfolioAnalyzer`.

## Strategy Pattern Contract

```csharp
public interface IPortfolioAnalyzer
{
    Task<List<PortfolioInsight>> AnalyzeAsync(
        PortfolioResponse portfolio,
        List<PortfolioSnapshotDto> historicalSnapshots
    );
}
```

**Inputs**:
- `PortfolioResponse` - Current portfolio state (positions, allocations, market values)
- `List<PortfolioSnapshotDto>` - Historical snapshots for trend analysis

**Output**: List of `PortfolioInsight` objects with:
- `Category` (Risk, Income, Efficiency, Trend)
- `Severity` (Info, Warning, Critical)
- `Message` (human-readable insight)
- `RecommendedAction` (optional actionable advice)

## Analyzer Inventory

| Analyzer | Category | Responsibilities |
|----------|----------|-----------------|
| **RiskAnalyzer** | Risk | Concentration risk (>20% Warning, >40% Critical), Diversification (HHI), Asset count |
| **IncomeAnalyzer** | Income | Dividend yield patterns, Income stream consistency, High-yield position identification |
| **EfficiencyAnalyzer** | Efficiency | Sharpe ratio, Return/risk ratios, Benchmark comparison (vs S&P 500) |
| **TrendAnalyzer** | Trend | Price momentum, Drawdown alerts, Portfolio growth trajectory |

## Threshold Boundaries (Must Be Tested)

### RiskAnalyzer Thresholds
- Position concentration **>20%** → Warning severity
- Position concentration **>40%** → Critical severity
- HHI (Herfindahl-Hirschman Index) calculation for diversification score
- Asset count recommendations (diversification target)

### IncomeAnalyzer Thresholds
- Dividend yield patterns (identify high-yield positions)
- Income consistency checks

### EfficiencyAnalyzer Constants
- Risk-free rate: **3%** (US Treasury approximation)
- Benchmark: **S&P 500 (`^GSPC`)**
- Trading days per year: **252**

### TrendAnalyzer Thresholds
- Momentum calculations based on historical snapshots
- Drawdown severity levels

## Usage Pattern

Analyzers are orchestrated by `PortfolioInsightsService`:

```csharp
public class PortfolioInsightsService
{
    private readonly IEnumerable<IPortfolioAnalyzer> _analyzers;

    public async Task<List<PortfolioInsight>> GetInsights(Guid userId)
    {
        var portfolio = await _portfolioService.GetPortfolioByUserId(userId);
        var snapshots = await _portfolioHistoryService.GetByDateRange(userId, ...);

        var insights = new List<PortfolioInsight>();
        foreach (var analyzer in _analyzers)
        {
            insights.AddRange(await analyzer.AnalyzeAsync(portfolio, snapshots));
        }

        return insights;
    }
}
```

All analyzers are injected via `IEnumerable<IPortfolioAnalyzer>` for automatic discovery.

## DI Registration

All analyzers registered in `Features/Investments/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IPortfolioAnalyzer, RiskAnalyzer>();
services.AddScoped<IPortfolioAnalyzer, IncomeAnalyzer>();
services.AddScoped<IPortfolioAnalyzer, EfficiencyAnalyzer>();
services.AddScoped<IPortfolioAnalyzer, TrendAnalyzer>();
```

## Testing Requirements

**Pattern**: Pure input → output tests (no mocks).

Each analyzer must have tests covering:

1. **Boundary conditions**: Test thresholds at, below, and above (e.g., 19.9%, 20%, 20.1%)
2. **Edge cases**: Empty portfolio, single position, fully diversified
3. **Severity levels**: Verify correct severity assigned for each threshold
4. **Message clarity**: Insights must be actionable and human-readable

### Example Test Pattern

```csharp
[Theory]
[InlineData(0.199, Severity.Info)]      // Below threshold
[InlineData(0.20, Severity.Warning)]    // At threshold
[InlineData(0.41, Severity.Critical)]   // Above critical
public async Task RiskAnalyzer_ConcentrationThresholds_CorrectSeverity(
    decimal concentration,
    Severity expectedSeverity)
{
    // Arrange
    var portfolio = CreatePortfolioWithConcentration(concentration);
    var analyzer = new RiskAnalyzer();

    // Act
    var insights = await analyzer.AnalyzeAsync(portfolio, new List<PortfolioSnapshotDto>());

    // Assert
    var concentrationInsight = insights.First(i => i.Message.Contains("concentration"));
    concentrationInsight.Severity.Should().Be(expectedSeverity);
}
```

## Adding a New Analyzer

1. Create class implementing `IPortfolioAnalyzer`
2. Add to DI registration
3. Define thresholds as constants at top of class
4. Add threshold documentation to this file (§ Threshold Boundaries)
5. Write boundary tests for all thresholds
6. Update `PortfolioInsight.Category` enum if needed

## Invariants

- **Thresholds must remain consistent** across analyzer implementation and tests
- **Analyzers must be stateless** (no instance fields except injected dependencies)
- **Insights must be actionable** (not just observations — tell user what to do)
- **Historical data may be empty** (analyzers must handle gracefully)
