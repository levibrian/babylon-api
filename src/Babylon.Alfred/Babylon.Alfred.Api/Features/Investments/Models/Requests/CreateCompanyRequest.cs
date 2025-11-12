using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class CreateCompanyRequest
{
    [Required]
    [MaxLength(50)]
    public string Ticker { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SecurityName { get; set; } = string.Empty;
}
