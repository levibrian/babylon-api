using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.Authentication.Models;

public class UpdatePasswordRequest
{
    public string? CurrentPassword { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
