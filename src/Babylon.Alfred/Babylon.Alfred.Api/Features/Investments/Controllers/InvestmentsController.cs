using Microsoft.AspNetCore.Mvc;
using Babylon.Alfred.Api.Features.Investments.DTOs;
using Babylon.Alfred.Api.Features.Investments.Services;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("/api/v1/investments")]
public class InvestmentsController(
    IInvestmentsService investmentsService,
    ILogger<InvestmentsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInvestmentSummary()
    {
        try
        {
            var summary = await investmentsService.GetInvestmentSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving investment summary");
            return Problem("An error occurred while retrieving the investment summary.");
        }
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetCurrentHoldings()
    {
        try
        {
            var holdings = await investmentsService.GetCurrentHoldingsAsync();
            return Ok(holdings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving current holdings");
            return Problem("An error occurred while retrieving current holdings.");
        }
    }

    [HttpGet("holdings/{assetSymbol}")]
    public async Task<IActionResult> GetAssetHolding(string assetSymbol)
    {
        try
        {
            var holding = await investmentsService.GetAssetHoldingAsync(assetSymbol);
            if (holding == null)
                return NotFound($"No holdings found for asset {assetSymbol}.");

            return Ok(holding);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving holding for asset {AssetSymbol}", assetSymbol);
            return Problem("An error occurred while retrieving the asset holding.");
        }
    }

    [HttpGet("assets")]
    public async Task<IActionResult> GetAllAssets()
    {
        try
        {
            var assets = await investmentsService.GetAllAssetsAsync();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving assets");
            return Problem("An error occurred while retrieving assets.");
        }
    }

    [HttpGet("assets/{symbol}")]
    public async Task<IActionResult> GetAssetBySymbol(string symbol)
    {
        try
        {
            var asset = await investmentsService.GetAssetBySymbolAsync(symbol);
            if (asset == null)
                return NotFound($"Asset with symbol {symbol} not found.");

            return Ok(asset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving asset {Symbol}", symbol);
            return Problem("An error occurred while retrieving the asset.");
        }
    }
}
