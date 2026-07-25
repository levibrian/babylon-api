using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Shared.Controllers;

[ApiController]
public abstract class BabylonControllerBase : ControllerBase
{
    protected IActionResult Success<T>(T data)
        => Ok(ApiResponse<T>.Ok(data));

    protected IActionResult Success()
        => Ok(ApiResponse<object>.Ok(new { }));

    protected IActionResult Created<T>(T data)
        => StatusCode(201, ApiResponse<T>.Ok(data));

    protected IActionResult Fail<T>(string error, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse<T>.Fail(error));

    protected IActionResult Fail(string error, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse<object>.Fail(error));
}
