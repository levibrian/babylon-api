using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Babylon.Alfred.Api.Features.Import.Controllers;
using Babylon.Alfred.Api.Features.Import.Models.Responses;
using Babylon.Alfred.Api.Features.Import.Services;
using Babylon.Alfred.Api.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Import;

public class ImportControllerTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly ImportController sut;
    private readonly Guid userId = Guid.NewGuid();

    public ImportControllerTests()
    {
        sut = autoMocker.CreateInstance<ImportController>();
        var identity = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) }, "TestAuthType");
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task ImportTradeRepublic_ShouldReturnOkWithResultForAuthenticatedUser()
    {
        // Arrange
        var csvContent = "\"type\"\n\"BUY\"";
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(csvContent)), 0, csvContent.Length, "file", "export.csv");
        var expected = new ImportResult { Imported = 1, SkippedDuplicates = 0 };
        autoMocker.GetMock<ICsvImportService>()
            .Setup(x => x.ImportTradeRepublicCsv(userId, csvContent))
            .ReturnsAsync(expected);

        // Act
        var result = await sut.ImportTradeRepublic(file);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ImportResult>>().Subject;
        apiResponse.Data.Should().BeSameAs(expected);
        autoMocker.GetMock<ICsvImportService>().Verify(x => x.ImportTradeRepublicCsv(userId, csvContent), Times.Once);
    }
}
