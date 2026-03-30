using Babylon.Alfred.Api.Features.Investments.Models.Responses.Dividends;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

/// <summary>
/// Controller for dividend history and income projections.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/portfolios/dividends")]
public class DividendsController(IDividendTrackerService dividendTrackerService) : ControllerBase
{
    /// <summary>
    /// Gets past dividend payouts and projected future dividends for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Returns the last 6 months of received dividends (filled bars) and the next 6 months
    /// of projected dividends (hollow bars) based on historical payout months and share count trend.
    /// </remarks>
    /// <returns>Dividend tracker with paid and projected monthly amounts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DividendTrackerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DividendTrackerResponse>> GetDividendTracker()
    {
        var userId = User.GetUserId();
        var result = await dividendTrackerService.GetDividendTracker(userId);
        return Ok(result);
    }
}
