using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.TestFeature.Controllers;

[ApiController]
[Route("/api/v1/summary")]
public class SummaryController(
    ILogger<SummaryController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string ticker)
    {
        logger.LogInformation("GET - Summary");
        
        return Ok("Success");
    }
}