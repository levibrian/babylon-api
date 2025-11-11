
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

public class CompaniesControllerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly CompaniesController sut;

    public CompaniesControllerTests()
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

        sut = autoMocker.CreateInstance<CompaniesController>();
    }

    [Fact]
    public async Task GetAllAsync_WhenCompaniesExist_ShouldReturnOkWithCompanies()
    {
        // Arrange
        var companies = fixture.CreateMany<CompanyDto>(3).ToList();
        autoMocker
            .GetMock<ICompanyService>()
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(companies);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompanies = okResult.Value.Should().BeAssignableTo<IList<CompanyDto>>().Subject;
        returnedCompanies.Should().HaveCount(3);
        returnedCompanies.Should().BeEquivalentTo(companies);
        autoMocker.GetMock<ICompanyService>().Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCompaniesExist_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        autoMocker
            .GetMock<ICompanyService>()
            .Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CompanyDto>());

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompanies = okResult.Value.Should().BeAssignableTo<IList<CompanyDto>>().Subject;
        returnedCompanies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTickerAsync_WhenCompanyExists_ShouldReturnOkWithCompany()
    {
        // Arrange
        var company = fixture.Create<CompanyDto>();
        autoMocker
            .GetMock<ICompanyService>()
            .Setup(x => x.GetByTickerAsync(company.Ticker)).ReturnsAsync(company);

        // Act
        var result = await sut.GetByTickerAsync(company.Ticker);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCompany = okResult.Value.Should().BeAssignableTo<CompanyDto>().Subject;
        returnedCompany.Should().BeEquivalentTo(company);
        autoMocker
            .GetMock<ICompanyService>()
            .Verify(x => x.GetByTickerAsync(company.Ticker), Times.Once);
    }

    [Fact]
    public async Task GetByTickerAsync_WhenCompanyDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker
            .GetMock<ICompanyService>().Setup(x => x.GetByTickerAsync(ticker)).ReturnsAsync((CompanyDto?)null);

        // Act
        var result = await sut.GetByTickerAsync(ticker);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = $"Company with ticker '{ticker}' not found" });
        autoMocker
            .GetMock<ICompanyService>().Verify(x => x.GetByTickerAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = fixture.Create<CreateCompanyRequest>();
        var createdCompany = fixture.Build<Company>()
            .With(c => c.Ticker, request.Ticker)
            .With(c => c.CompanyName, request.CompanyName)
            .Create();
        autoMocker
            .GetMock<ICompanyService>().Setup(x => x.CreateAsync(request)).ReturnsAsync(createdCompany);

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(CompaniesController.GetByTickerAsync));
        createdResult.RouteValues.Should().ContainKey("ticker");
        createdResult.RouteValues!["ticker"].Should().Be(request.Ticker);
        createdResult.Value.Should().BeEquivalentTo(createdCompany);
        autoMocker
            .GetMock<ICompanyService>().Verify(x => x.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCompanyExists_ShouldReturnOkWithUpdatedCompany()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        var request = fixture.Create<UpdateCompanyRequest>();
        var updatedCompany = fixture.Build<Company>()
            .With(c => c.Ticker, ticker)
            .With(c => c.CompanyName, request.CompanyName)
            .Create();
        autoMocker
            .GetMock<ICompanyService>().Setup(x => x.UpdateAsync(ticker, request)).ReturnsAsync(updatedCompany);

        // Act
        var result = await sut.UpdateAsync(ticker, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updatedCompany);
        autoMocker
            .GetMock<ICompanyService>().Verify(x => x.UpdateAsync(ticker, request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCompanyDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        var request = fixture.Create<UpdateCompanyRequest>();
        autoMocker
            .GetMock<ICompanyService>().Setup(x => x.UpdateAsync(ticker, request)).ReturnsAsync((Company?)null);

        // Act
        var result = await sut.UpdateAsync(ticker, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = $"Company with ticker '{ticker}' not found" });
        autoMocker
            .GetMock<ICompanyService>().Verify(x => x.UpdateAsync(ticker, request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyExists_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker
            .GetMock<ICompanyService>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = $"Company with ticker '{ticker}' successfully deleted" });
        autoMocker
            .GetMock<ICompanyService>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker
            .GetMock<ICompanyService>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(false);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = $"Company with ticker '{ticker}' not found" });
        autoMocker
            .GetMock<ICompanyService>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }
}
