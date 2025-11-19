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
        var security = fixture.Build<Security>()
            .With(c => c.Ticker, request.Ticker)
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .Create();

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(security);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.Add(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await sut.Create(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(transaction);
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(request.Ticker), Times.Once);
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
    public async Task Create_WhenSecurityNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = fixture.Build<CreateTransactionRequest>()
            .With(r => r.SharesQuantity, 10m)
            .With(r => r.SharePrice, 100m)
            .Create();

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync((Security?)null);

        // Act
        Func<Task> act = async () => await sut.Create(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Security provided not found in our internal database.");
        autoMocker.GetMock<ISecurityRepository>().Verify(x => x.GetByTickerAsync(request.Ticker), Times.Once);
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
        var security = fixture.Build<Security>()
            .With(c => c.Ticker, request.Ticker)
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(security);
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
        var security = fixture.Build<Security>()
            .With(c => c.Ticker, request.Ticker)
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var beforeCreate = DateTime.UtcNow;

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickerAsync(request.Ticker))
            .ReturnsAsync(security);
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

        // Mock securities for bulk create
        var securities = requests.Select(r => fixture.Build<Security>()
            .With(c => c.Ticker, r.Ticker)
            .With(c => c.Id, fixture.Create<Guid>())
            .Create()).ToList();
        
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> tickers) =>
            {
                return securities.Where(c => tickers.Contains(c.Ticker))
                    .ToDictionary(c => c.Ticker, c => c);
            });

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
            .With(r => r.UserId, fixture.Create<Guid>())
            .Create();
        var requests = new List<CreateTransactionRequest> { request };

        // Mock security for bulk create
        var security = fixture.Build<Security>()
            .With(c => c.Ticker, request.Ticker)
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> tickers) =>
            {
                return new Dictionary<string, Security> { { request.Ticker, security } };
            });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.AddBulk(It.IsAny<IList<Transaction?>>()))
            .ReturnsAsync((IList<Transaction?> transactions) => transactions);

        // Act
        var result = await sut.CreateBulk(requests);

        // Assert
        var transaction = result.First();
        transaction.SecurityId.Should().Be(security.Id);
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

        // Mock securities for bulk create
        var securities = requests.Select(r => fixture.Build<Security>()
            .With(c => c.Ticker, r.Ticker)
            .With(c => c.Id, fixture.Create<Guid>())
            .Create()).ToList();
        
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByTickersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> tickers) =>
            {
                return securities.Where(c => tickers.Contains(c.Ticker))
                    .ToDictionary(c => c.Ticker, c => c);
            });

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.AddBulk(It.IsAny<IList<Transaction?>>()))
            .ReturnsAsync((IList<Transaction?> transactions) => transactions);

        // Act
        var result = await sut.CreateBulk(requests);

        // Assert
        result.Should().AllSatisfy(t => t.UserId.Should().Be(Constants.User.RootUserId));
    }

    [Fact]
    public async Task GetAllByUser_WithUserId_ShouldReturnTransactionsOrderedByDateDescending()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var security1 = fixture.Build<Security>()
            .With(s => s.Ticker, "AAPL")
            .With(s => s.SecurityName, "Apple Inc.")
            .Create();
        var security2 = fixture.Build<Security>()
            .With(s => s.Ticker, "GOOGL")
            .With(s => s.SecurityName, "Alphabet Inc.")
            .Create();

        var oldDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var middleDate = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        // Repository returns transactions ordered by date descending, so mock should return them in that order
        var transactions = new List<Transaction>
        {
            fixture.Build<Transaction>()
                .With(t => t.Id, fixture.Create<Guid>())
                .With(t => t.SecurityId, security2.Id)
                .With(t => t.Security, security2)
                .With(t => t.TransactionType, TransactionType.Sell)
                .With(t => t.Date, newDate)
                .With(t => t.SharesQuantity, 5m)
                .With(t => t.SharePrice, 2800m)
                .With(t => t.Fees, 10m)
                .With(t => t.UserId, userId)
                .Create(),
            fixture.Build<Transaction>()
                .With(t => t.Id, fixture.Create<Guid>())
                .With(t => t.SecurityId, security1.Id)
                .With(t => t.Security, security1)
                .With(t => t.TransactionType, TransactionType.Buy)
                .With(t => t.Date, middleDate)
                .With(t => t.SharesQuantity, 20m)
                .With(t => t.SharePrice, 160m)
                .With(t => t.Fees, 8m)
                .With(t => t.UserId, userId)
                .Create(),
            fixture.Build<Transaction>()
                .With(t => t.Id, fixture.Create<Guid>())
                .With(t => t.SecurityId, security1.Id)
                .With(t => t.Security, security1)
                .With(t => t.TransactionType, TransactionType.Buy)
                .With(t => t.Date, oldDate)
                .With(t => t.SharesQuantity, 10m)
                .With(t => t.SharePrice, 150m)
                .With(t => t.Fees, 5m)
                .With(t => t.UserId, userId)
                .Create()
        };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(transactions);

        // Act
        var result = (await sut.GetAllByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(t => t.Date);
        result[0].Date.Should().Be(newDate);
        result[1].Date.Should().Be(middleDate);
        result[2].Date.Should().Be(oldDate);
        result[0].Ticker.Should().Be("GOOGL");
        result[0].SecurityName.Should().Be("Alphabet Inc.");
        result[1].Ticker.Should().Be("AAPL");
        result[2].Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetAllByUser_WithNullUserId_ShouldUseRootUserId()
    {
        // Arrange
        var transactions = fixture.CreateMany<Transaction>(0).ToList();
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(Constants.User.RootUserId))
            .ReturnsAsync(transactions);

        // Act
        await sut.GetAllByUser(null);

        // Assert
        autoMocker.GetMock<ITransactionRepository>().Verify(
            x => x.GetAllByUser(Constants.User.RootUserId),
            Times.Once);
    }

    [Fact]
    public async Task GetAllByUser_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var transactionId = fixture.Create<Guid>();
        var security = fixture.Build<Security>()
            .With(s => s.Ticker, "MSFT")
            .With(s => s.SecurityName, "Microsoft Corporation")
            .Create();
        var date = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);
        var transaction = fixture.Build<Transaction>()
            .With(t => t.Id, transactionId)
            .With(t => t.SecurityId, security.Id)
            .With(t => t.Security, security)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.Date, date)
            .With(t => t.SharesQuantity, 15m)
            .With(t => t.SharePrice, 350m)
            .With(t => t.Fees, 7.5m)
            .With(t => t.UserId, userId)
            .Create();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(new List<Transaction> { transaction });

        // Act
        var result = (await sut.GetAllByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(transactionId);
        dto.Ticker.Should().Be("MSFT");
        dto.SecurityName.Should().Be("Microsoft Corporation");
        dto.Date.Should().Be(date);
        dto.SharesQuantity.Should().Be(15m);
        dto.SharePrice.Should().Be(350m);
        dto.Fees.Should().Be(7.5m);
        dto.TransactionType.Should().Be(TransactionType.Buy);
        dto.TotalAmount.Should().Be(5257.5m); // (15 * 350) + 7.5
    }

    [Fact]
    public async Task GetAllByUser_WithNoTransactions_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(fixture.CreateMany<Transaction>(0).ToList());

        // Act
        var result = await sut.GetAllByUser(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByUser_WithNullSecurity_ShouldHandleGracefully()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.Id, fixture.Create<Guid>())
            .With(t => t.SecurityId, fixture.Create<Guid>())
            .With(t => t.Security, (Security)null!)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.Date, DateTime.UtcNow)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 100m)
            .With(t => t.Fees, 5m)
            .With(t => t.UserId, userId)
            .Create();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(new List<Transaction> { transaction });

        // Act
        var result = (await sut.GetAllByUser(userId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Ticker.Should().BeEmpty();
        result[0].SecurityName.Should().BeEmpty();
    }
}

