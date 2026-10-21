using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Profiling.Models.Responses;
using Babylon.Alfred.Api.Features.Profiling.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class InvestorProfileTool
{
    [McpServerTool(Name = "get_profiling_guide", ReadOnly = true)]
    [Description("Returns the question guide and rubric for running a Babylon investor-profiling interview. Not scoped to a user — same guide for everyone.")]
    public static string GetProfilingGuide()
        => ReadGuideFile(Path.Combine(AppContext.BaseDirectory, "profiling-guide.md"));

    [McpServerTool(Name = "get_profile", ReadOnly = true)]
    [Description("Returns the signed-in Babylon user's investor profile. All fields are null if no profile has been saved yet.")]
    public static Task<InvestorProfileDto> GetProfile(ClaimsPrincipal? user, IInvestorProfileService profileService)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return profileService.GetProfile(user.GetUserId());
    }

    [McpServerTool(Name = "save_profile", Idempotent = true)]
    [Description("Saves the signed-in Babylon user's investor profile. Only call this after the user has reviewed and confirmed the full profileMarkdown you intend to save.")]
    public static Task<InvestorProfileDto> SaveProfile(
        ClaimsPrincipal? user,
        IInvestorProfileService profileService,
        [Description("Full profile markdown with the fixed section headings from the profiling guide")] string profileMarkdown,
        [Description("Raw interview Q&A transcript")] string rawAnswers,
        [Description("Rubric version used, e.g. \"v1\"")] string rubricVersion)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return profileService.SaveProfile(user.GetUserId(), profileMarkdown, rawAnswers, rubricVersion);
    }

    public static string ReadGuideFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Profiling guide file not found", path);
        }

        return File.ReadAllText(path);
    }
}
