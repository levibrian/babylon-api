using System.Security.Claims;
using Babylon.Alfred.Api.Features.Profiling.Models.Responses;
using Babylon.Alfred.Api.Features.Profiling.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class InvestorProfileToolTests
{
    [Fact]
    public void ReadGuideFile_WhenFileExists_ReturnsContent()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "# Babylon profiling guide");

            var result = InvestorProfileTool.ReadGuideFile(path);

            result.Should().Be("# Babylon profiling guide");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadGuideFile_WhenFileMissing_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var act = () => InvestorProfileTool.ReadGuideFile(path);

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public async Task GetProfile_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var expected = new InvestorProfileDto { ProfileMarkdown = "## Risk Tolerance\nModerate" };
        var profileService = new Mock<IInvestorProfileService>();
        profileService.Setup(x => x.GetProfile(userId)).ReturnsAsync(expected);

        var result = await InvestorProfileTool.GetProfile(user, profileService.Object);

        result.Should().BeSameAs(expected);
        profileService.Verify(x => x.GetProfile(userId), Times.Once);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthenticatedUser_Throws()
    {
        var profileService = new Mock<IInvestorProfileService>();

        Func<Task> act = () => InvestorProfileTool.GetProfile(new ClaimsPrincipal(new ClaimsIdentity()), profileService.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SaveProfile_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var expected = new InvestorProfileDto { ProfileMarkdown = "markdown" };
        var profileService = new Mock<IInvestorProfileService>();
        profileService.Setup(x => x.SaveProfile(userId, "markdown", "answers", "v1")).ReturnsAsync(expected);

        var result = await InvestorProfileTool.SaveProfile(user, profileService.Object, "markdown", "answers", "v1");

        result.Should().BeSameAs(expected);
        profileService.Verify(x => x.SaveProfile(userId, "markdown", "answers", "v1"), Times.Once);
    }

    [Fact]
    public async Task SaveProfile_WithoutAuthenticatedUser_Throws()
    {
        var profileService = new Mock<IInvestorProfileService>();

        Func<Task> act = () => InvestorProfileTool.SaveProfile(new ClaimsPrincipal(new ClaimsIdentity()), profileService.Object, "m", "a", "v1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
