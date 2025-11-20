using System.ComponentModel.DataAnnotations;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Models.Requests;

public class UpdateCompanyRequest
{
    [Required]
    [MaxLength(100)]
    public string SecurityName { get; set; } = string.Empty;

    public SecurityType? SecurityType { get; set; }
}
