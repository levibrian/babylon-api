using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class UpdateCompanyRequest
{
    [Required]
    [MaxLength(100)]
    public string CompanyName { get; set; } = string.Empty;
}
