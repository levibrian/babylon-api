using System.ComponentModel.DataAnnotations;

namespace Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;

public class CreateRecurringScheduleRequest
{
    [Required]
    [MaxLength(50)]
    public string Ticker { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SecurityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Platform { get; set; }

    public decimal? TargetAmount { get; set; }
}

