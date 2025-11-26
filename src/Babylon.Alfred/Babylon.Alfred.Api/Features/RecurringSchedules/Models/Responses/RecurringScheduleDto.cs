namespace Babylon.Alfred.Api.Features.RecurringSchedules.Models.Responses;

public class RecurringScheduleDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string SecurityName { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public decimal? TargetAmount { get; set; }
}

