using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/companies")]
public class CompaniesController(ICompanyService companyService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var companies = await companyService.GetAllAsync();
        return Ok(companies);
    }

    [HttpGet("{ticker}")]
    public async Task<IActionResult> GetByTickerAsync(string ticker)
    {
        var company = await companyService.GetByTickerAsync(ticker);
        if (company == null)
        {
            return NotFound(new { message = $"Company with ticker '{ticker}' not found" });
        }
        return Ok(company);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateCompanyRequest request)
    {
        var company = await companyService.CreateAsync(request);
        return CreatedAtAction(
            nameof(GetByTickerAsync),
            new { ticker = company.Ticker },
            company
        );
    }

    [HttpPut("{ticker}")]
    public async Task<IActionResult> UpdateAsync(string ticker, UpdateCompanyRequest request)
    {
        var company = await companyService.UpdateAsync(ticker, request);
        if (company == null)
        {
            return NotFound(new { message = $"Company with ticker '{ticker}' not found" });
        }
        return Ok(company);
    }

    [HttpDelete("{ticker}")]
    public async Task<IActionResult> DeleteAsync(string ticker)
    {
        var result = await companyService.DeleteAsync(ticker);
        if (!result)
        {
            return NotFound(new { message = $"Company with ticker '{ticker}' not found" });
        }
        return Ok(new { message = $"Company with ticker '{ticker}' successfully deleted" });
    }
}
