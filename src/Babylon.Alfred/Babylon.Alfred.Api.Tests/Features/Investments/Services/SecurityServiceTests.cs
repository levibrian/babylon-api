
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

public class SecurityServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly SecurityService sut;

    public SecurityServiceTests()
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

        sut = autoMocker.CreateInstance<SecurityService>();
    }

    [Fact]
    public async Task GetAllAsync_WhenSecuritiesExist_ShouldReturnAllSecurities()
    {
        // Arrange
        var securities = fixture.CreateMany<Security>(3).ToList();
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetAllAsync()).ReturnsAsync(securities);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllBeOfType<CompanyDto>();
        result.Select(x => x.Ticker).Should().BeEquivalentTo(securities.Select(c => c.Ticker));
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSecuritiesExist_ShouldReturnEmptyList()
    {
        // Arrange
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Security>());

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByTickerAsync_WhenSecurityExists_ShouldReturnSecurityDto()
    {
        // Arrange
        var security = fixture.Create<Security>();
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetByTickerAsync(security.Ticker)).ReturnsAsync(security);

        // Act
        var result = await sut.GetByTickerAsync(security.Ticker);

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be(security.Ticker);
        result.CompanyName.Should().Be(security.CompanyName);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(security.Ticker), Times.Once);
    }

    [Fact]
    public async Task GetByTickerAsync_WhenSecurityDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetByTickerAsync(ticker)).ReturnsAsync((Security?)null);

        // Act
        Func<Task> act = async () => await sut.GetByTickerAsync(ticker);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Security provided not found in our internal database.");
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateAndReturnSecurity()
    {
        // Arrange
        var request = fixture.Create<CreateCompanyRequest>();
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Security>()))
            .ReturnsAsync((Security c) => c);

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be(request.Ticker);
        result.CompanyName.Should().Be(request.CompanyName);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.AddOrUpdateAsync(
            It.Is<Security>(c =>
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

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Security>()))
            .ReturnsAsync((Security c) => c);

        // Act
        var result = await sut.CreateAsync(request);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.LastUpdated.Should().NotBeNull();
        result.LastUpdated.Should().BeOnOrAfter(beforeCreate);
        result.LastUpdated.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task UpdateAsync_WhenSecurityExists_ShouldUpdateAndReturnSecurity()
    {
        // Arrange
        var existingSecurity = fixture.Build<Security>()
            .With(c => c.LastUpdated, DateTime.UtcNow.AddDays(-1))
            .Create();
        var request = fixture.Create<UpdateCompanyRequest>();

        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetByTickerAsync(existingSecurity.Ticker)).ReturnsAsync(existingSecurity);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Security>()))
            .ReturnsAsync((Security c) => c);

        // Act
        var result = await sut.UpdateAsync(existingSecurity.Ticker, request);

        // Assert
        result.Should().NotBeNull();
        result!.Ticker.Should().Be(existingSecurity.Ticker);
        result.CompanyName.Should().Be(request.CompanyName);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(existingSecurity.Ticker), Times.Once);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.AddOrUpdateAsync(existingSecurity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSecurityDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        var request = fixture.Create<UpdateCompanyRequest>();
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetByTickerAsync(ticker)).ReturnsAsync((Security?)null);

        // Act
        var result = await sut.UpdateAsync(ticker, request);

        // Assert
        result.Should().BeNull();
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(ticker), Times.Once);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.AddOrUpdateAsync(It.IsAny<Security>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var oldTimestamp = DateTime.UtcNow.AddDays(-10);
        var existingSecurity = fixture.Build<Security>()
            .With(c => c.LastUpdated, oldTimestamp)
            .Create();
        var request = fixture.Create<UpdateCompanyRequest>();

        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetByTickerAsync(existingSecurity.Ticker)).ReturnsAsync(existingSecurity);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Security>()))
            .ReturnsAsync((Security c) => c);

        // Act
        var result = await sut.UpdateAsync(existingSecurity.Ticker, request);

        // Assert
        result.Should().NotBeNull();
        result!.LastUpdated.Should().NotBe(oldTimestamp);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteAsync_WhenSecurityExists_ShouldReturnTrue()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(true);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        result.Should().BeTrue();
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSecurityDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var ticker = fixture.Create<string>();
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.DeleteAsync(ticker)).ReturnsAsync(false);

        // Act
        var result = await sut.DeleteAsync(ticker);

        // Assert
        result.Should().BeFalse();
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.DeleteAsync(ticker), Times.Once);
    }
}

