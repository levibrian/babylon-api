using System.Security.Claims;
using Babylon.Alfred.Api.Features.Import.Models.Responses;
using Babylon.Alfred.Api.Features.Import.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class ImportToolTests
{
    [Fact]
    public async Task ImportTradeRepublicCsv_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var csvContent = "\"type\"\n\"BUY\"";
        var expected = new ImportResult { Imported = 1, SkippedDuplicates = 0 };
        var csvImportService = new Mock<ICsvImportService>();
        csvImportService.Setup(x => x.ImportTradeRepublicCsv(userId, csvContent)).ReturnsAsync(expected);

        var result = await ImportTool.ImportTradeRepublicCsv(user, csvImportService.Object, csvContent);

        result.Should().BeSameAs(expected);
        csvImportService.Verify(x => x.ImportTradeRepublicCsv(userId, csvContent), Times.Once);
    }

    [Fact]
    public async Task ImportTradeRepublicCsv_WithoutAuthenticatedUser_Throws()
    {
        var csvImportService = new Mock<ICsvImportService>();

        Func<Task> act = () => ImportTool.ImportTradeRepublicCsv(new ClaimsPrincipal(new ClaimsIdentity()), csvImportService.Object, "csv");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
