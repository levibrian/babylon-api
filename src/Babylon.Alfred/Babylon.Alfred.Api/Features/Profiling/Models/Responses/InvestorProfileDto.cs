namespace Babylon.Alfred.Api.Features.Profiling.Models.Responses;

public class InvestorProfileDto
{
    public string? ProfileMarkdown { get; init; }
    public string? RawAnswers { get; init; }
    public string? RubricVersion { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
