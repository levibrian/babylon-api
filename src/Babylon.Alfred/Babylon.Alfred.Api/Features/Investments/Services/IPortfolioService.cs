using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IPortfolioService
{
    public Task<PortfolioResponse> GetPortfolio(Guid userId);
}
