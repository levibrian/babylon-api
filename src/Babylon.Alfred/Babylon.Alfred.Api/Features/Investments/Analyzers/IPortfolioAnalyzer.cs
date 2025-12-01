using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Analyzers;

/// <summary>
/// Interface for portfolio analyzers that generate strategic insights.
/// </summary>
public interface IPortfolioAnalyzer
{
    Task<IEnumerable<PortfolioInsightDto>> AnalyzeAsync(PortfolioResponse portfolio, List<Transaction> history);
}

