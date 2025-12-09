using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/market")]
public class MarketController(IYahooMarketDataService yahooMarketDataService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Query parameter is required" });
        }

        try
        {
            var yahooResults = await yahooMarketDataService.SearchAsync(query);
            
            // Map Yahoo results to our DTO
            var results = yahooResults.Select(r => new SearchResultDto
            {
                Ticker = r.Symbol,
                Name = !string.IsNullOrWhiteSpace(r.LongName) ? r.LongName : r.ShortName,
                Type = r.QuoteType,
                Exchange = r.Exchange
            }).ToList();
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            // In a real app, log the exception
            return StatusCode(500, new { message = "An error occurred while searching for tickers", error = ex.Message });
        }
    }
}
