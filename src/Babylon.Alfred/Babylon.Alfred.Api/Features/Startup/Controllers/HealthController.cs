using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Startup.Controllers;

[Route("/health")]
public class HealthController : BabylonControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Success<object>(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}
