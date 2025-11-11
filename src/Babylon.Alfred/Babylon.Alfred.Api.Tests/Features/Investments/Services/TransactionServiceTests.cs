using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class TransactionServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly TransactionService sut;

    public TransactionServiceTests()
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

        sut = autoMocker.CreateInstance<TransactionService>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateAndReturnTransaction()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .With(r => r.Fees, 5m)
            .Create();
        var company = fixture.Build<Company>()
            .With(c => c.Ticker, request.Ticker)
            .Create();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.Ticker, request.Ticker)
            .Create();

        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(company);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.Add(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await sut.Create(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(transaction);
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetByTickerAsync(request.Ticker), Times.Once);
        autoMocker.GetMock<ITransactionRepository>().Verify(x => x.Add(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithNullTicker_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.Ticker, (string)null!)
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Ticker cannot be null or empty*");
    }

    [Fact]
    public async Task Create_WithEmptyTicker_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.Ticker, string.Empty)
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Ticker cannot be null or empty*");
    }

    [Fact]
    public async Task Create_WithWhitespaceTicker_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.Ticker, "   ")
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Ticker cannot be null or empty*");
    }

    [Fact]
    public async Task Create_WithZeroSharesQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 0m)
            .With(r => r.SharePrice, 100m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("SharesQuantity must be greater than zero*");
    }

    [Fact]
    public async Task Create_WithNegativeSharesQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, -10m)
            .With(r => r.SharePrice, 100m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("SharesQuantity must be greater than zero*");
    }

    [Fact]
    public async Task Create_WithZeroSharePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 0m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("SharePrice must be greater than zero*");
    }

    [Fact]
    public async Task Create_WithNegativeSharePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, -100m)
            .Create();

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("SharePrice must be greater than zero*");
    }

    [Fact]
    public async Task Create_WhenCompanyNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .Create();

        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync((Company?)null);

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Company provided not found in our internal database.");
        autoMocker.GetMock<ICompanyRepository>().Verify(x => x.GetByTickerAsync(request.Ticker), Times.Once);
        autoMocker.GetMock<ITransactionRepository>().Verify(x => x.Add(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithNullUserId_ShouldUseRootUserId()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .With(r => r.UserId, (Guid?)null)
            .Create();
        var company = fixture.Build<Company>()
            .With(c => c.Ticker, request.Ticker)
            .Create();

        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(company);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.Add(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        // Act
        var result = await sut.Create(request);

        // Assert
        autoMocker.GetMock<ITransactionRepository>().Verify(
            x => x.Add(It.Is<Transaction>(t => t.UserId == Constants.User.RootUserId)),
            Times.Once);
    }

    [Fact]
    public async Task Create_WithNullDate_ShouldUseCurrentUtcTime()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .With(r => r.Date, (DateOnly?)null)
            .Create();
        var company = fixture.Build<Company>()
            .With(c => c.Ticker, request.Ticker)
            .Create();
        var beforeCreate = DateTime.UtcNow;

        autoMocker.GetMock<ICompanyRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(company);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.Add(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        // Act
        await sut.Create(request);
        var afterCreate = DateTime.UtcNow;

        // Assert
        autoMocker.GetMock<ITransactionRepository>().Verify(
            x => x.Add(It.Is<Transaction>(t => 
                t.Date >= beforeCreate && 
                t.Date <= afterCreate)),
            Times.Once);
    }

    [Fact]
    public async Task CreateBulk_WithValidRequests_ShouldCreateMultipleTransactions()
    {
        // Arrange
        var requests = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .CreateMany(5)
            .ToList();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.AddBulk(It.IsAny<IList<Transaction?>>()))
            .ReturnsAsync((IList<Transaction?> transactions) => transactions);

        // Act
        var result = await sut.CreateBulk(requests);

        // Assert
        result.Should().HaveCount(5);
        result.Should().AllBeOfType<Transaction>();
        autoMocker.GetMock<ITransactionRepository>().Verify(
            x => x.AddBulk(It.Is<IList<Transaction?>>(list => list.Count == 5)),
            Times.Once);
    }

    [Fact]
    public async Task CreateBulk_WithEmptyList_ShouldReturnEmptyListAndNotCallRepository()
    {
        // Arrange
        var requests = new List<CreateTransactionRequest>();

        // Act
        var result = await sut.CreateBulk(requests);

        // Assert
        result.Should().BeEmpty();
        autoMocker.GetMock<ITransactionRepository>().Verify(
            x => x.AddBulk(It.IsAny<IList<Transaction?>>()), 
            Times.Never);
        autoMocker.GetMock<ILogger<TransactionService>>().Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No transactions to create")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBulk_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.Ticker, "AAPL")
            .With(r => r.TransactionType, TransactionType.Buy)
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 150m)
            .With(r => r.Fees, 5m)
            .With(r => r.Date, new DateOnly(2025, 1, 15))
            .With(r => r.UserId, Guid.NewGuid())
            .Create();
        var requests = new List<CreateTransactionRequest> { request };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.AddBulk(It.IsAny<IList<Transaction?>>()))
            .ReturnsAsync((IList<Transaction?> transactions) => transactions);

        // Act
        var result = await sut.CreateBulk(requests);

        // Assert
        var transaction = result.First();
        transaction.Ticker.Should().Be("AAPL");
        transaction.TransactionType.Should().Be(TransactionType.Buy);
        transaction.SharesQuantity.Should().Be(10m);
        transaction.SharePrice.Should().Be(150m);
        transaction.Fees.Should().Be(5m);
        transaction.Date.Should().Be(new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        transaction.UserId.Should().Be(request.UserId);
    }

    [Fact]
    public async Task CreateBulk_WithNullUserIds_ShouldUseRootUserId()
    {
        // Arrange
        var requests = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .With(r => r.UserId, (Guid?)null)
            .CreateMany(3)
            .ToList();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.AddBulk(It.IsAny<IList<Transaction?>>()))
            .ReturnsAsync((IList<Transaction?> transactions) => transactions);

        // Act
        var result = await sut.CreateBulk(requests);

        // Assert
        result.Should().AllSatisfy(t => t.UserId.Should().Be(Constants.User.RootUserId));
    }
}

