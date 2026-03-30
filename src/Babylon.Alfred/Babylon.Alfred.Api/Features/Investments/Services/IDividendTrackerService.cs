using Babylon.Alfred.Api.Features.Investments.Models.Responses.Dividends;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IDividendTrackerService
{
    Task<DividendTrackerResponse> GetDividendTracker(Guid userId);
}
