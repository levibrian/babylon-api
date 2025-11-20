
using AutoFixture;
using AutoFixture.AutoMoq;
using Babylon.Alfred.Api.Features.Investments.Controllers;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Controllers;

public class SecuritiesControllerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly SecuritiesController sut;

    public SecuritiesControllerTests()
    {
        // Configure AutoFixture to handle recursive types
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Configure AutoFixture to handle DateOnly - prevents invalid date generation
        fixture.Customize<DateOnly>(composer => composer.FromFactory(() =>
        {
            var random = new Random();
            var year = random.Next(2020, 2030);
            var month = random.Next(1, 13);
            var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
            return new DateOnly(year, month, day);
        }));

        sut = autoMocker.CreateInstance<SecuritiesController>();
    }

    [Fact]
    public async Task GetAllAsync_WhenSecuritiesExist_ShouldReturnOkWithSecurities()
    {
        // Arrange
        var securities = fixture.CreateMany<CompanyDto>(3).ToList();
        autoMocker
            .GetMock<ISecurityService>()
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(securities);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSecurities = okResult.Value.Should().BeAssignableTo<IList<CompanyDto>>().Subject;
        returnedSecurities.Should().HaveCount(3);
        returnedSecurities.Should().BeEquivalentTo(securities);
        autoMocker.GetMock<ISecurityService>().Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSecuritiesExist_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        autoMocker
            .GetMock<ISecurityService>()
            .Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CompanyDto>());

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSecurities = okResult.Value.Should().BeAssignableTo<IList<CompanyDto>>().Subject;
        returnedSecurities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTickerAsync_WhenSecurityExists_ShouldReturnOkWithSecurity()
    {
        // Arrange
        var security = fixture.Create<CompanyDto>();
        autoMocker
            .GetMock<ISecurityService>()
            .Setup(x => x.GetByTickerAsync(security.Ticker)).ReturnsAsync(security);

        // Act
        var result = await sut.GetByTickerAsync(security.Ticker);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSecurity = okResult.Value.Should().BeAssignableTo<CompanyDto>().Subject;
        returnedSecurity.Should().BeEquivalentTo(security);
        autoMocker
            .GetMock<ISecurityService>()
            .Verify(x => x.GetByTickerAsync(security.Ticker), Times.Once);
    }

    [Fact]
    public async Task GetByTickerAsync_WhenSecurityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker
            .GetMock<ISecurityService>().Setup(x => x.GetByTickerAsync(ticker)).ReturnsAsync((CompanyDto?)null);

        // Act
        var result = await sut.GetByTickerAsync(ticker);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = $"Security with ticker '{ticker}' not found" });
        autoMocker
            .GetMock<ISecurityService>().Verify(x => x.GetByTickerAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = fixture.Create<CreateCompanyRequest>();
        var createdSecurity = fixture.Build<Security>()
            .With(c => c.Ticker, request.Ticker)
            .With(c => c.SecurityName, request.SecurityName)
            .With(c => c.SecurityType, request.SecurityType)
            .Create();
        autoMocker
            .GetMock<ISecurityService>().Setup(x => x.CreateAsync(request)).ReturnsAsync(createdSecurity);

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SecuritiesController.GetByTickerAsync));
        createdResult.RouteValues.Should().ContainKey("ticker");
        createdResult.RouteValues!["ticker"].Should().Be(request.Ticker);
        createdResult.Value.Should().BeEquivalentTo(createdSecurity);
        autoMocker
            .GetMock<ISecurityService>().Verify(x => x.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSecurityExists_ShouldReturnOkWithUpdatedSecurity()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        var request = fixture.Create<UpdateCompanyRequest>();
        var updatedSecurity = fixture.Build<Security>()
            .With(c => c.Ticker, ticker)
            .With(c => c.SecurityName, request.SecurityName)
            .With(c => c.SecurityType, request.SecurityType ?? SecurityType.Stock)
            .Create();
        autoMocker
            .GetMock<ISecurityService>().Setup(x => x.UpdateAsync(ticker, request)).ReturnsAsync(updatedSecurity);

        // Act
        var result = await sut.UpdateAsync(ticker, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updatedSecurity);
        autoMocker
            .GetMock<ISecurityService>().Verify(x => x.UpdateAsync(ticker, request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSecurityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        var request = fixture.Create<UpdateCompanyRequest>();
        autoMocker
            .GetMock<ISecurityService>().Setup(x => x.UpdateAsync(ticker, request)).ReturnsAsync((Security?)null);

        // Act
        var result = await sut.UpdateAsync(ticker, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = $"Security with ticker '{ticker}' not found" });
        autoMocker
            .GetMock<ISecurityService>().Verify(x => x.UpdateAsync(ticker, request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSecurityExists_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker
            .GetMock<ISecurityService>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = $"Security with ticker '{ticker}' successfully deleted" });
        autoMocker
            .GetMock<ISecurityService>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSecurityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker
            .GetMock<ISecurityService>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(false);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = $"Security with ticker '{ticker}' not found" });
        autoMocker
            .GetMock<ISecurityService>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }
}

