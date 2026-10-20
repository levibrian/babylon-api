using Babylon.Alfred.Api.Features.Import.Models.Responses;
using Babylon.Alfred.Api.Features.Import.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Import.Controllers;

[Authorize]
[Route("api/v1/imports")]
public class ImportController(ICsvImportService csvImportService) : BabylonControllerBase
{
    [HttpPost("trade-republic")]
    public async Task<ActionResult<ApiResponse<ImportResult>>> ImportTradeRepublic(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync();

        var result = await csvImportService.ImportTradeRepublicCsv(User.GetUserId(), csvContent);
        return Success(result);
    }
}
