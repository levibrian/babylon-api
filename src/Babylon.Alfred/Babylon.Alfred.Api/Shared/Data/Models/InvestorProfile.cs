namespace Babylon.Alfred.Api.Shared.Data.Models;

public class InvestorProfile
{
    public Guid UserId { get; set; }
    public string? ProfileMarkdown { get; set; }
    public string? RawAnswers { get; set; }
    public string? RubricVersion { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
