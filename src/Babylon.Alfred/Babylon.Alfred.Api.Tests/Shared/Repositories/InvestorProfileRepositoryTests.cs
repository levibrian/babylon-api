using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Tests.Shared.Repositories;

public class InvestorProfileRepositoryTests : IDisposable
{
    private readonly BabylonDbContext context;
    private readonly InvestorProfileRepository sut;

    public InvestorProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BabylonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new BabylonDbContext(options);
        sut = new InvestorProfileRepository(context);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoProfileExists_ShouldReturnNull()
    {
        // Act
        var result = await sut.GetByUserIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenNoProfileExists_ShouldInsert()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new InvestorProfile
        {
            UserId = userId,
            ProfileMarkdown = "## Risk Tolerance\nModerate",
            RawAnswers = "Q: ... A: ...",
            RubricVersion = "v1",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await sut.AddOrUpdateAsync(profile);

        // Assert
        var saved = await context.InvestorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        saved.Should().NotBeNull();
        saved!.ProfileMarkdown.Should().Be("## Risk Tolerance\nModerate");
        saved.RubricVersion.Should().Be("v1");
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenProfileExists_ShouldUpdateInPlace()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await context.InvestorProfiles.AddAsync(new InvestorProfile
        {
            UserId = userId,
            ProfileMarkdown = "old",
            RawAnswers = "old answers",
            RubricVersion = "v1",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var updated = new InvestorProfile
        {
            UserId = userId,
            ProfileMarkdown = "new",
            RawAnswers = "new answers",
            RubricVersion = "v2",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await sut.AddOrUpdateAsync(updated);

        // Assert
        var all = await context.InvestorProfiles.Where(p => p.UserId == userId).ToListAsync();
        all.Should().ContainSingle();
        all[0].ProfileMarkdown.Should().Be("new");
        all[0].RubricVersion.Should().Be("v2");
    }
}
