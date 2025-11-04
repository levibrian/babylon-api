using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Controllers;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Controllers;

public class PortfoliosControllerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly PortfoliosController sut;

    public PortfoliosControllerTests()
    {
        sut = autoMocker.CreateInstance<PortfoliosController>();
    }

    [Fact]
    public async Task Get_WithUserId_ShouldReturnOkWithPortfolio()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var portfolioResponse = fixture.Create<PortfolioResponse>();
        autoMocker
            .GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolioResponse);

        // Act
        var result = await sut.Get(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPortfolio = okResult.Value.Should().BeAssignableTo<PortfolioResponse>().Subject;
        returnedPortfolio.Should().BeEquivalentTo(portfolioResponse);
        autoMocker.GetMock<IPortfolioService>().Verify(x => x.GetPortfolio(userId), Times.Once);
    }

    [Fact]
    public async Task Get_WithNullUserId_ShouldReturnOkWithPortfolio()
    {
        // Arrange
        Guid? userId = null;
        var portfolioResponse = fixture.Create<PortfolioResponse>();
        autoMocker
            .GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolioResponse);

        // Act
        var result = await sut.Get(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPortfolio = okResult.Value.Should().BeAssignableTo<PortfolioResponse>().Subject;
        returnedPortfolio.Should().BeEquivalentTo(portfolioResponse);
        autoMocker.GetMock<IPortfolioService>().Verify(x => x.GetPortfolio(userId), Times.Once);
    }

    [Fact]
    public async Task Get_WhenPortfolioIsEmpty_ShouldReturnOkWithEmptyPortfolio()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyPortfolio = new PortfolioResponse
        {
            Positions = new List<PortfolioPositionDto>(),
            TotalInvested = 0
        };
        autoMocker
            .GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(emptyPortfolio);

        // Act
        var result = await sut.Get(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPortfolio = okResult.Value.Should().BeAssignableTo<PortfolioResponse>().Subject;
        returnedPortfolio.Positions.Should().BeEmpty();
        returnedPortfolio.TotalInvested.Should().Be(0);
        autoMocker.GetMock<IPortfolioService>().Verify(x => x.GetPortfolio(userId), Times.Once);
    }

    [Fact]
    public async Task Get_WithValidUserId_ShouldReturnPortfolioWithMultiplePositions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var positions = fixture.CreateMany<PortfolioPositionDto>(3).ToList();
        var portfolioResponse = new PortfolioResponse
        {
            Positions = positions,
            TotalInvested = positions.Sum(p => p.TotalInvested)
        };
        autoMocker
            .GetMock<IPortfolioService>()
            .Setup(x => x.GetPortfolio(userId))
            .ReturnsAsync(portfolioResponse);

        // Act
        var result = await sut.Get(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPortfolio = okResult.Value.Should().BeAssignableTo<PortfolioResponse>().Subject;
        returnedPortfolio.Positions.Should().HaveCount(3);
        returnedPortfolio.TotalInvested.Should().Be(positions.Sum(p => p.TotalInvested));
        autoMocker.GetMock<IPortfolioService>().Verify(x => x.GetPortfolio(userId), Times.Once);
    }
}

