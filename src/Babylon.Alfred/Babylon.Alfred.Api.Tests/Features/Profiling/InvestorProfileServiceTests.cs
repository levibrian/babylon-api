using Babylon.Alfred.Api.Features.Profiling.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Profiling;

public class InvestorProfileServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly InvestorProfileService sut;

    public InvestorProfileServiceTests()
    {
        sut = autoMocker.CreateInstance<InvestorProfileService>();
    }

    [Fact]
    public async Task GetProfile_WhenNoProfileExists_ShouldReturnAllNullDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        autoMocker.GetMock<IInvestorProfileRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((InvestorProfile?)null);

        // Act
        var result = await sut.GetProfile(userId);

        // Assert
        result.ProfileMarkdown.Should().BeNull();
        result.RawAnswers.Should().BeNull();
        result.RubricVersion.Should().BeNull();
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetProfile_WhenProfileExists_ShouldReturnMappedDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        autoMocker.GetMock<IInvestorProfileRepository>()
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new InvestorProfile
            {
                UserId = userId,
                ProfileMarkdown = "## Risk Tolerance\nModerate",
                RawAnswers = "Q&A",
                RubricVersion = "v1",
                UpdatedAt = updatedAt
            });

        // Act
        var result = await sut.GetProfile(userId);

        // Assert
        result.ProfileMarkdown.Should().Be("## Risk Tolerance\nModerate");
        result.RawAnswers.Should().Be("Q&A");
        result.RubricVersion.Should().Be("v1");
        result.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task SaveProfile_WithValidInput_ShouldUpsertAndReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        InvestorProfile? captured = null;
        autoMocker.GetMock<IInvestorProfileRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<InvestorProfile>()))
            .Callback<InvestorProfile>(p => captured = p)
            .ReturnsAsync((InvestorProfile p) => p);

        // Act
        var result = await sut.SaveProfile(userId, "markdown", "answers", "v1");

        // Assert
        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.ProfileMarkdown.Should().Be("markdown");
        captured.RawAnswers.Should().Be("answers");
        captured.RubricVersion.Should().Be("v1");
        result.ProfileMarkdown.Should().Be("markdown");
    }

    [Theory]
    [InlineData(null, "answers", "v1")]
    [InlineData("", "answers", "v1")]
    [InlineData("markdown", null, "v1")]
    [InlineData("markdown", "answers", "")]
    public async Task SaveProfile_WithMissingRequiredField_ShouldThrowArgumentException(
        string? profileMarkdown, string? rawAnswers, string? rubricVersion)
    {
        // Act
        var act = () => sut.SaveProfile(Guid.NewGuid(), profileMarkdown!, rawAnswers!, rubricVersion!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
