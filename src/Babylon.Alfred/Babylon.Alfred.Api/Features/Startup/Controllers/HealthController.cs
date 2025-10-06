using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Startup.Controllers;

[ApiController]
[Route("/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}
