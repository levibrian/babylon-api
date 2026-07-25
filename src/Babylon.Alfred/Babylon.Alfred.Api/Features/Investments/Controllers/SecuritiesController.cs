using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[Route("api/v1/securities")]
public class SecuritiesController(ISecurityService securityService) : BabylonControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<CompanyDto>>>> GetAllAsync()
    {
        var securities = await securityService.GetAllAsync();
        return Success(securities);
    }

    [HttpGet("{ticker}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetByTickerAsync(string ticker)
    {
        var security = await securityService.GetByTickerAsync(ticker);
        if (security == null)
        {
            return Fail<CompanyDto>($"Security with ticker '{ticker}' not found", 404);
        }
        return Success(security);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> CreateAsync(CreateSecurityByTickerRequest request)
    {
        var security = await securityService.CreateOrGetByTickerAsync(request.Ticker);
        return Success(security);
    }

    /// <summary>
    /// Admin endpoint: Create a security with full metadata (without Yahoo Finance lookup)
    /// </summary>
    [HttpPost("admin")]
    public async Task<ActionResult<ApiResponse<Security>>> CreateAdminAsync(CreateCompanyRequest request)
    {
        var security = await securityService.CreateAsync(request);
        return Created(security);
    }

    [HttpPut("{ticker}")]
    public async Task<ActionResult<ApiResponse<Security>>> UpdateAsync(string ticker, UpdateCompanyRequest request)
    {
        var security = await securityService.UpdateAsync(ticker, request);
        if (security == null)
        {
            return Fail<Security>($"Security with ticker '{ticker}' not found", 404);
        }
        return Success(security);
    }

    [HttpDelete("{ticker}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(string ticker)
    {
        var result = await securityService.DeleteAsync(ticker);
        if (!result)
        {
            return Fail($"Security with ticker '{ticker}' not found", 404);
        }
        return Success();
    }
}
