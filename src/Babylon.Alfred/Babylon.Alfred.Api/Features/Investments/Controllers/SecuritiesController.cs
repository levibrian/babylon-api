using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/securities")]
public class SecuritiesController(ISecurityService securityService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var securities = await securityService.GetAllAsync();
        return Ok(securities);
    }

    [HttpGet("{ticker}")]
    public async Task<IActionResult> GetByTickerAsync(string ticker)
    {
        var security = await securityService.GetByTickerAsync(ticker);
        if (security == null)
        {
            return NotFound(new { message = $"Security with ticker '{ticker}' not found" });
        }
        return Ok(security);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateCompanyRequest request)
    {
        var security = await securityService.CreateAsync(request);
        return CreatedAtAction(
            nameof(GetByTickerAsync),
            new { ticker = security.Ticker },
            security
        );
    }

    [HttpPut("{ticker}")]
    public async Task<IActionResult> UpdateAsync(string ticker, UpdateCompanyRequest request)
    {
        var security = await securityService.UpdateAsync(ticker, request);
        if (security == null)
        {
            return NotFound(new { message = $"Security with ticker '{ticker}' not found" });
        }
        return Ok(security);
    }

    [HttpDelete("{ticker}")]
    public async Task<IActionResult> DeleteAsync(string ticker)
    {
        var result = await securityService.DeleteAsync(ticker);
        if (!result)
        {
            return NotFound(new { message = $"Security with ticker '{ticker}' not found" });
        }
        return Ok(new { message = $"Security with ticker '{ticker}' successfully deleted" });
    }
}

