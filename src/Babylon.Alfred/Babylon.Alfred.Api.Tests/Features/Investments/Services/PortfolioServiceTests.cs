using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Services;

public class PortfolioServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly PortfolioService sut;

    public PortfolioServiceTests()
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

        sut = autoMocker.CreateInstance<PortfolioService>();

        // Setup default mocks for services that are always needed
        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>());
        autoMocker.GetMock<IAllocationStrategyService>()
            .Setup(x => x.GetTargetAllocationsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Dictionary<string, decimal>());
    }

    [Fact]
    public async Task GetPortfolio_WithNoTransactions_ShouldReturnEmptyPortfolio()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var emptyTransactions = new List<Transaction>();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(emptyTransactions);

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().BeEmpty();
        result.TotalInvested.Should().Be(0);
        autoMocker.GetMock<ITransactionRepository>().Verify(x => x.GetOpenPositionsByUser(userId), Times.Once);
    }

    [Fact]
    public async Task GetPortfolio_WithNullUserId_ShouldUseRootUserId()
    {
        // Arrange
        Guid? userId = null;
        var transactions = new List<Transaction>();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(Constants.User.RootUserId))
            .ReturnsAsync(transactions);

        // Act
        await sut.GetPortfolio(userId);

        // Assert
        autoMocker.GetMock<ITransactionRepository>()
            .Verify(x => x.GetOpenPositionsByUser(Constants.User.RootUserId), Times.Once);
    }

    [Fact]
    public async Task GetPortfolio_WithSingleTransaction_ShouldReturnPortfolioWithOnePosition()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var company = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(company.Id))
                {
                    result.Add(company);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().HaveCount(1);
        result.Positions.First().Ticker.Should().Be("AAPL");
        result.Positions.First().SecurityName.Should().Be("Apple Inc.");
        result.Positions.First().TotalInvested.Should().Be(transaction.TotalAmount);
        result.Positions.First().Transactions.Should().HaveCount(1);
        result.TotalInvested.Should().Be(transaction.TotalAmount);
    }

    [Fact]
    public async Task GetPortfolio_WithMultipleTransactionsSameTicker_ShouldGroupByTicker()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var company = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var transaction1 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.Date, new DateTime(2025, 1, 1))
            .With(t => t.UserId, userId)
            .Create();
        var transaction2 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 160m)
            .With(t => t.Fees, 3m)
            .With(t => t.Date, new DateTime(2025, 2, 1))
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction1, transaction2 };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(company.Id))
                {
                    result.Add(company);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().HaveCount(1);
        result.Positions.First().Ticker.Should().Be("AAPL");
        result.Positions.First().Transactions.Should().HaveCount(2);
        result.Positions.First().TotalInvested.Should().Be(transaction1.TotalAmount + transaction2.TotalAmount);
        result.TotalInvested.Should().Be(transaction1.TotalAmount + transaction2.TotalAmount);
    }

    [Fact]
    public async Task GetPortfolio_WithMultipleTransactionsDifferentTickers_ShouldCreateSeparatePositions()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var securityApple = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var securityGoogle = fixture.Build<Security>()
            .With(c => c.Ticker, "GOOGL")
            .With(c => c.SecurityName, "Alphabet Inc.")
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var transactionApple = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityApple.Id)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.UserId, userId)
            .Create();
        var transactionGoogle = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityGoogle.Id)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 2800m)
            .With(t => t.Fees, 10m)
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transactionApple, transactionGoogle };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(securityApple.Id))
                {
                    result.Add(securityApple);
                }
                if (securityIdList.Contains(securityGoogle.Id))
                {
                    result.Add(securityGoogle);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().HaveCount(2);
        result.Positions.Should().Contain(p => p.Ticker == "AAPL" && p.SecurityName == "Apple Inc.");
        result.Positions.Should().Contain(p => p.Ticker == "GOOGL" && p.SecurityName == "Alphabet Inc.");
        result.TotalInvested.Should().Be(transactionApple.TotalAmount + transactionGoogle.TotalAmount);
    }

    [Fact]
    public async Task GetPortfolio_WhenSecurityNotFound_ShouldUseFallbackTickerAsSecurityName()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var securityId = fixture.Create<Guid>();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityId)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 100m)
            .With(t => t.Fees, 5m)
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Security>()); // Empty list = company not found

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().HaveCount(1);
        result.Positions.First().Ticker.Should().BeEmpty(); // No company found, so ticker is empty
        result.Positions.First().SecurityName.Should().BeEmpty(); // Fallback to empty string
    }

    [Fact]
    public async Task GetPortfolio_ShouldOrderPositionsByTotalInvestedDescending()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var securitySmall = fixture.Build<Security>().With(c => c.Ticker, "SMALL").With(c => c.Id, Guid.NewGuid()).Create();
        var securityLarge = fixture.Build<Security>().With(c => c.Ticker, "LARGE").With(c => c.Id, Guid.NewGuid()).Create();
        var securityMedium = fixture.Build<Security>().With(c => c.Ticker, "MEDIUM").With(c => c.Id, Guid.NewGuid()).Create();
        var transactionSmall = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securitySmall.Id)
            .With(t => t.SharesQuantity, 1m)
            .With(t => t.SharePrice, 100m)
            .With(t => t.Fees, 1m)
            .With(t => t.UserId, userId)
            .Create();
        var transactionLarge = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityLarge.Id)
            .With(t => t.SharesQuantity, 100m)
            .With(t => t.SharePrice, 1000m)
            .With(t => t.Fees, 50m)
            .With(t => t.UserId, userId)
            .Create();
        var transactionMedium = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityMedium.Id)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 500m)
            .With(t => t.Fees, 10m)
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transactionSmall, transactionLarge, transactionMedium };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(securitySmall.Id))
                {
                    result.Add(securitySmall);
                }
                if (securityIdList.Contains(securityLarge.Id))
                {
                    result.Add(securityLarge);
                }
                if (securityIdList.Contains(securityMedium.Id))
                {
                    result.Add(securityMedium);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().HaveCount(3);
        result.Positions[0].Ticker.Should().Be("LARGE");
        result.Positions[1].Ticker.Should().Be("MEDIUM");
        result.Positions[2].Ticker.Should().Be("SMALL");
        result.Positions.Should().BeInDescendingOrder(p => p.TotalInvested);
    }

    [Fact]
    public async Task GetPortfolio_ShouldOrderTransactionsWithinPositionByDateDescending()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var company = fixture.Build<Security>().With(c => c.Ticker, "AAPL").With(c => c.Id, Guid.NewGuid()).Create();
        var oldTransaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.Date, new DateTime(2024, 1, 1))
            .With(t => t.UserId, userId)
            .Create();
        var newTransaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 160m)
            .With(t => t.Fees, 3m)
            .With(t => t.Date, new DateTime(2025, 1, 1))
            .With(t => t.UserId, userId)
            .Create();
        var middleTransaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.SharesQuantity, 7m)
            .With(t => t.SharePrice, 155m)
            .With(t => t.Fees, 4m)
            .With(t => t.Date, new DateTime(2024, 6, 1))
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { oldTransaction, newTransaction, middleTransaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(company.Id))
                {
                    result.Add(company);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.Positions.Should().HaveCount(1);
        var position = result.Positions.First();
        position.Transactions.Should().HaveCount(3);
        position.Transactions.Should().BeInDescendingOrder(t => t.Date);
        position.Transactions[0].Date.Should().Be(new DateTime(2025, 1, 1));
        position.Transactions[1].Date.Should().Be(new DateTime(2024, 6, 1));
        position.Transactions[2].Date.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task GetPortfolio_ShouldMapAllTransactionPropertiesCorrectly()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var transactionId = fixture.Create<Guid>();
        var company = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, fixture.Create<Guid>())
            .Create();
        var transaction = new Transaction
        {
            Id = transactionId,
            SecurityId = company.Id,
            TransactionType = TransactionType.Buy,
            Date = new DateTime(2025, 1, 15),
            SharesQuantity = 10m,
            SharePrice = 150m,
            Fees = 5m,
            UserId = userId
        };
        var transactions = new List<Transaction> { transaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(company.Id))
                {
                    result.Add(company);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        var mappedTransaction = result.Positions.First().Transactions.First();
        mappedTransaction.Id.Should().Be(transactionId);
        mappedTransaction.TransactionType.Should().Be(TransactionType.Buy);
        mappedTransaction.Date.Should().Be(new DateTime(2025, 1, 15));
        mappedTransaction.SharesQuantity.Should().Be(10m);
        mappedTransaction.SharePrice.Should().Be(150m);
        mappedTransaction.Fees.Should().Be(5m);
        mappedTransaction.TotalAmount.Should().Be(1505m); // 1500 + 5
    }

    [Fact]
    public async Task GetPortfolio_ShouldCalculateTotalInvestedCorrectly()
    {
        // Arrange
        var userId = fixture.Create<Guid>();
        var securityApple = fixture.Build<Security>().With(c => c.Ticker, "AAPL").With(c => c.Id, Guid.NewGuid()).Create();
        var securityGoogle = fixture.Build<Security>().With(c => c.Ticker, "GOOGL").With(c => c.Id, Guid.NewGuid()).Create();
        var transaction1 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityApple.Id)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m) // TotalAmount = 1505
            .With(t => t.UserId, userId)
            .Create();
        var transaction2 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityGoogle.Id)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 2800m)
            .With(t => t.Fees, 10m) // TotalAmount = 14010
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction1, transaction2 };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> securityIds) =>
            {
                var securityIdList = securityIds.ToList();
                var result = new List<Security>();
                if (securityIdList.Contains(securityApple.Id))
                {
                    result.Add(securityApple);
                }
                if (securityIdList.Contains(securityGoogle.Id))
                {
                    result.Add(securityGoogle);
                }
                return result;
            });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalInvested.Should().Be(1505m + 14010m); // Total of all transactions
        result.Positions.Sum(p => p.TotalInvested).Should().Be(result.TotalInvested);
    }
}

