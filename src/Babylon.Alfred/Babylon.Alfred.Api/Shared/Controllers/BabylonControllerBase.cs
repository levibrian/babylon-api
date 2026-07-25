using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Shared.Controllers;

[ApiController]
public abstract class BabylonControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data)
        => Ok(ApiResponse<T>.Ok(data));

    protected ActionResult<ApiResponse<object>> Success()
        => Ok(ApiResponse<object>.Ok(new { }));

    protected ActionResult<ApiResponse<T>> Created<T>(T data)
        => StatusCode(201, ApiResponse<T>.Ok(data));

    protected ActionResult<ApiResponse<T>> Fail<T>(string error, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse<T>.Fail(error));

    protected ActionResult<ApiResponse<object>> Fail(string error, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse<object>.Fail(error));
}
