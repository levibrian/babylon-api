
using AutoFixture;
using AutoFixture.AutoMoq;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class CompanyServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly CompanyService sut;

    public CompanyServiceTests()
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

        sut = autoMocker.CreateInstance<CompanyService>();
    }

    [Fact]
    public async Task GetAllAsync_WhenCompaniesExist_ShouldReturnAllCompanies()
    {
        // Arrange
        var companies = fixture.CreateMany<Company>(3).ToList();
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetAllAsync()).ReturnsAsync(companies);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllBeOfType<CompanyDto>();
        result.Select(x => x.Ticker).Should().BeEquivalentTo(companies.Select(c => c.Ticker));
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCompaniesExist_ShouldReturnEmptyList()
    {
        // Arrange
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Company>());

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByTickerAsync_WhenCompanyExists_ShouldReturnCompanyDto()
    {
        // Arrange
        var company = fixture.Create<Company>();
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetByTickerAsync(company.Ticker)).ReturnsAsync(company);

        // Act
        var result = await sut.GetByTickerAsync(company.Ticker);

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be(company.Ticker);
        result.CompanyName.Should().Be(company.CompanyName);
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetByTickerAsync(company.Ticker), Times.Once);
    }

    [Fact]
    public async Task GetByTickerAsync_WhenCompanyDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetByTickerAsync(ticker)).ReturnsAsync((Company?)null);

        // Act
        Func<Task> act = async () => await sut.GetByTickerAsync(ticker);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Company provided not found in our internal database.");
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetByTickerAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateAndReturnCompany()
    {
        // Arrange
        var request = fixture.Create<CreateCompanyRequest>();
        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => c);

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be(request.Ticker);
        result.CompanyName.Should().Be(request.CompanyName);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.AddOrUpdateAsync(
            It.Is<Company>(c =>
                c.Ticker == request.Ticker &&
                c.CompanyName == request.CompanyName &&
                c.LastUpdated != null)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetLastUpdatedToUtcNow()
    {
        // Arrange
        var request = fixture.Create<CreateCompanyRequest>();
        var beforeCreate = DateTime.UtcNow;

        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => c);

        // Act
        var result = await sut.CreateAsync(request);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.LastUpdated.Should().NotBeNull();
        result.LastUpdated.Should().BeOnOrAfter(beforeCreate);
        result.LastUpdated.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task UpdateAsync_WhenCompanyExists_ShouldUpdateAndReturnCompany()
    {
        // Arrange
        var existingCompany = fixture.Build<Company>()
            .With(c => c.LastUpdated, DateTime.UtcNow.AddDays(-1))
            .Create();
        var request = fixture.Create<UpdateCompanyRequest>();

        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetByTickerAsync(existingCompany.Ticker)).ReturnsAsync(existingCompany);
        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => c);

        // Act
        var result = await sut.UpdateAsync(existingCompany.Ticker, request);

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be(existingCompany.Ticker);
        result.CompanyName.Should().Be(request.CompanyName);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetByTickerAsync(existingCompany.Ticker), Times.Once);
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.AddOrUpdateAsync(existingCompany), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCompanyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        var request = fixture.Create<UpdateCompanyRequest>();
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetByTickerAsync(ticker)).ReturnsAsync((Company?)null);

        // Act
        var result = await sut.UpdateAsync(ticker, request);

        // Assert
        result.Should().BeNull();
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetByTickerAsync(ticker), Times.Once);
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.AddOrUpdateAsync(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var oldTimestamp = DateTime.UtcNow.AddDays(-10);
        var existingCompany = fixture.Build<Company>()
            .With(c => c.LastUpdated, oldTimestamp)
            .Create();
        var request = fixture.Create<UpdateCompanyRequest>();

        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.GetByTickerAsync(existingCompany.Ticker)).ReturnsAsync(existingCompany);
        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => c);

        // Act
        var result = await sut.UpdateAsync(existingCompany.Ticker, request);

        // Assert
        result.Should().NotBeNull();
        result!.LastUpdated.Should().NotBe(oldTimestamp);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyExists_ShouldReturnTrue()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        result.Should().BeTrue();
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker.GetMock<ICompanyRepository>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(false);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        result.Should().BeFalse();
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }
}
