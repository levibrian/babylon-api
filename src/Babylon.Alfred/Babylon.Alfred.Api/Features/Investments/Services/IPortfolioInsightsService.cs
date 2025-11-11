using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IPortfolioInsightsService
{
    Task<List<PortfolioInsightDto>> GetTopInsightsAsync(Guid userId, int count = 5);
}

