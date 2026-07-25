using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
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

        // Force Tax to be 0 for all objects that have it
        fixture.Customize<Transaction>(c => c.With(t => t.Tax, 0m));
        fixture.Customize<CreateTransactionRequest>(c => c.With(t => t.Tax, 0m));

        sut = autoMocker.CreateInstance<PortfolioService>();

        // Setup default mocks for services that are always needed
        autoMocker.GetMock<IMarketPriceService>()
            .Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal>());
        autoMocker.GetMock<ICashBalanceService>()
            .Setup(x => x.GetBalanceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0m);
    }

    [Fact]
    public async Task GetPortfolio_WithNoTransactions_ShouldReturnEmptyPortfolio()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyTransactions = new List<Transaction>();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(emptyTransactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
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
    public async Task GetPortfolio_WithSingleTransaction_ShouldReturnPortfolioWithOnePosition()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var company = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, Guid.NewGuid())
            .Create();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.Tax, 0m)  // Tax is not included in Buy cost basis
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
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
        result.TotalInvested.Should().Be(transaction.TotalAmount);
    }

    [Fact]
    public async Task GetPortfolio_WithMultipleTransactionsSameTicker_ShouldGroupByTicker()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var company = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, Guid.NewGuid())
            .Create();
        var transaction1 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.Tax, 0m)  // Tax is not included in Buy cost basis
            .With(t => t.Date, new DateTime(2025, 1, 1))
            .With(t => t.UserId, userId)
            .Create();
        var transaction2 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, company.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 160m)
            .With(t => t.Fees, 3m)
            .With(t => t.Tax, 0m)  // Tax is not included in Buy cost basis
            .With(t => t.Date, new DateTime(2025, 2, 1))
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction1, transaction2 };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
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
        result.Positions.First().TotalInvested.Should().Be(transaction1.TotalAmount + transaction2.TotalAmount);
        result.TotalInvested.Should().Be(transaction1.TotalAmount + transaction2.TotalAmount);
    }

    [Fact]
    public async Task GetPortfolio_WithMultipleTransactionsDifferentTickers_ShouldCreateSeparatePositions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityApple = fixture.Build<Security>()
            .With(c => c.Ticker, "AAPL")
            .With(c => c.SecurityName, "Apple Inc.")
            .With(c => c.Id, Guid.NewGuid())
            .Create();
        var securityGoogle = fixture.Build<Security>()
            .With(c => c.Ticker, "GOOGL")
            .With(c => c.SecurityName, "Alphabet Inc.")
            .With(c => c.Id, Guid.NewGuid())
            .Create();
        var transactionApple = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityApple.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m)
            .With(t => t.Tax, 0m)  // Tax is not included in Buy cost basis
            .With(t => t.UserId, userId)
            .Create();
        var transactionGoogle = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityGoogle.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 2800m)
            .With(t => t.Fees, 10m)
            .With(t => t.Tax, 0m)  // Tax is not included in Buy cost basis
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transactionApple, transactionGoogle };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
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
        var userId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityId)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 100m)
            .With(t => t.Fees, 5m)
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
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
    public async Task GetPortfolio_ShouldCalculateTotalInvestedCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityApple = fixture.Build<Security>().With(c => c.Ticker, "AAPL").With(c => c.Id, Guid.NewGuid()).Create();
        var securityGoogle = fixture.Build<Security>().With(c => c.Ticker, "GOOGL").With(c => c.Id, Guid.NewGuid()).Create();
        var transaction1 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityApple.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 150m)
            .With(t => t.Fees, 5m) // TotalAmount = 1505
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();
        var transaction2 = fixture.Build<Transaction>()
            .With(t => t.SecurityId, securityGoogle.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 5m)
            .With(t => t.SharePrice, 2800m)
            .With(t => t.Fees, 10m) // TotalAmount = 14010
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();
        var transactions = new List<Transaction> { transaction1, transaction2 };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(transactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
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

    [Fact]
    public async Task GetPortfolio_AfterPartialSell_TotalInvestedShouldReflectCurrentPositionNotHistoricalBuys()
    {
        // Arrange — buy 10 @ 10 (no fees) = invested 100, then sell 2 @ 10.
        // TotalInvested must drop to 80 (current remaining cost basis), NOT stay at 100
        // (historical gross buys) and NOT be affected by the sell price/proceeds.
        var userId = Guid.NewGuid();
        var security = fixture.Build<Security>().With(c => c.Ticker, "AAPL").With(c => c.Id, Guid.NewGuid()).Create();
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var buyTransaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.Date, baseDate)
            .With(t => t.CreatedAt, baseDate)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 10m)
            .With(t => t.Fees, 0m)
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();
        var sellTransaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .With(t => t.TransactionType, TransactionType.Sell)
            .With(t => t.Date, baseDate.AddDays(1))
            .With(t => t.CreatedAt, baseDate.AddDays(1))
            .With(t => t.SharesQuantity, 2m)
            .With(t => t.SharePrice, 10m)
            .With(t => t.Fees, 0m)
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();

        // Position is still open (8 shares remain) so it appears in GetOpenPositionsByUser
        var openPositionTransactions = new List<Transaction> { buyTransaction };
        var allTransactions = new List<Transaction> { buyTransaction, sellTransaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(openPositionTransactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(allTransactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Security> { security });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Positions.Should().HaveCount(1);
        result.Positions.First().TotalShares.Should().Be(8m);
        result.Positions.First().TotalInvested.Should().Be(80m);
        result.TotalInvested.Should().Be(80m);
    }

    [Fact]
    public async Task GetPortfolio_AfterSellAcrossMultipleLotsAtDifferentPrices_TotalInvestedShouldReflectFifoRemainingCost()
    {
        // Arrange — two buys at different prices (different lots), then a sell that spans
        // both lots. TotalInvested must reflect the FIFO-remaining cost basis of what's left,
        // not a simple average of historical buys and not the sell proceeds.
        //
        // Buy A: 10 @ 10, fee 0  -> cost 100 (10/share)
        // Buy B: 10 @ 20, fee 0  -> cost 200 (20/share)
        // Sell 15 @ 50: consumes all 10 from Lot A (cost 100) + 5 from Lot B (cost 100)
        //   costBasisConsumed = 200
        // Remaining: 5 shares from Lot B, cost 100
        var userId = Guid.NewGuid();
        var security = fixture.Build<Security>().With(c => c.Ticker, "AAPL").With(c => c.Id, Guid.NewGuid()).Create();
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var buyA = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.Date, baseDate)
            .With(t => t.CreatedAt, baseDate)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 10m)
            .With(t => t.Fees, 0m)
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();
        var buyB = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.Date, baseDate.AddDays(1))
            .With(t => t.CreatedAt, baseDate.AddDays(1))
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 20m)
            .With(t => t.Fees, 0m)
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();
        var sellTransaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .With(t => t.TransactionType, TransactionType.Sell)
            .With(t => t.Date, baseDate.AddDays(2))
            .With(t => t.CreatedAt, baseDate.AddDays(2))
            .With(t => t.SharesQuantity, 15m)
            .With(t => t.SharePrice, 50m)
            .With(t => t.Fees, 0m)
            .With(t => t.Tax, 0m)
            .With(t => t.UserId, userId)
            .Create();

        var openPositionTransactions = new List<Transaction> { buyA, buyB };
        var allTransactions = new List<Transaction> { buyA, buyB, sellTransaction };

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetOpenPositionsByUser(userId))
            .ReturnsAsync(openPositionTransactions);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetAllByUser(userId))
            .ReturnsAsync(allTransactions);
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Security> { security });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Positions.Should().HaveCount(1);
        result.Positions.First().TotalShares.Should().Be(5m);
        result.Positions.First().TotalInvested.Should().Be(100m);
        result.TotalInvested.Should().Be(100m);
    }

    [Fact]
    public async Task GetPortfolio_ShouldIncludeCashInTotalMarketValueButNotAsPosition()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cashAmount = 5000m;

        autoMocker.GetMock<ITransactionRepository>().Setup(x => x.GetOpenPositionsByUser(userId)).ReturnsAsync(new List<Transaction>());
        autoMocker.GetMock<ICashBalanceService>().Setup(x => x.GetBalanceAsync(userId)).ReturnsAsync(cashAmount);

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        result.Should().NotBeNull();
        result.CashAmount.Should().Be(cashAmount);
        result.TotalMarketValue.Should().Be(null);
        result.TotalInvested.Should().Be(0);
        result.Positions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPortfolio_CashNotShouldAffectAllocationPercentagesOfOtherPositions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cashAmount = 1000m;

        var security = fixture.Build<Security>().With(s => s.Ticker, "AAPL").With(s => s.Id, Guid.NewGuid()).Create();
        var transaction = fixture.Build<Transaction>()
            .With(t => t.SecurityId, security.Id)
            .With(t => t.TransactionType, TransactionType.Buy)
            .With(t => t.SharesQuantity, 10m)
            .With(t => t.SharePrice, 100m)
            .With(t => t.Fees, 0m)
            .With(t => t.UserId, userId)
            .Create();

        autoMocker.GetMock<ITransactionRepository>().Setup(x => x.GetOpenPositionsByUser(userId)).ReturnsAsync(new List<Transaction> { transaction });
        autoMocker.GetMock<ITransactionRepository>().Setup(x => x.GetAllByUser(userId)).ReturnsAsync(new List<Transaction> { transaction });
        autoMocker.GetMock<ISecurityRepository>().Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(new List<Security> { security });
        autoMocker.GetMock<ICashBalanceService>().Setup(x => x.GetBalanceAsync(userId)).ReturnsAsync(cashAmount);

        // Market Price for AAPL is 100
        autoMocker.GetMock<IMarketPriceService>().Setup(x => x.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { { "AAPL", 100m } });

        // Act
        var result = await sut.GetPortfolio(userId);

        // Assert
        // Total Assets (AAPL) = 1000
        // Cash = 1000
        // Total Market Value = 1000
        // AAPL Allocation should be 1000/2000 = 50%

        result.Should().NotBeNull();
        result.TotalMarketValue.Should().Be(1000m);
        result.Positions.Should().HaveCount(1);
        result.Positions.First().Ticker.Should().Be("AAPL");
        result.Positions.First().CurrentAllocationPercentage.Should().Be(50m);
    }
}

