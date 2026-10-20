using Babylon.Alfred.Api.Features.Import.Services;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Import;

public class CsvImportServiceTests
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "Features", "Import", "Fixtures", "trade-republic-sample.csv");

    private readonly AutoMocker autoMocker = new();
    private readonly CsvImportService sut;

    public CsvImportServiceTests()
    {
        sut = autoMocker.CreateInstance<CsvImportService>();

        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetExistingImportFingerprintsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new HashSet<string>());

        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIsinAsync("XX0000000001"))
            .ReturnsAsync(new Security { Id = Guid.NewGuid(), Ticker = "WORLD", SecurityName = "Fixture World ETF" });
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIsinAsync("XX0000000002"))
            .ReturnsAsync(new Security { Id = Guid.NewGuid(), Ticker = "DIVCO", SecurityName = "Fixture Dividend Co" });
        autoMocker.GetMock<ISecurityRepository>()
            .Setup(x => x.GetByIsinAsync("XX9999999999"))
            .ReturnsAsync((Security?)null);

        autoMocker.GetMock<ITransactionService>()
            .Setup(x => x.CreateBulk(It.IsAny<Guid>(), It.IsAny<List<CreateTransactionRequest>>()))
            .ReturnsAsync(new List<Transaction>());
    }

    [Fact]
    public async Task ImportTradeRepublicCsv_ResolvesKnownSecuritiesAndReportsUnknownIsinAsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var csv = await File.ReadAllTextAsync(FixturePath);

        // Act
        var result = await sut.ImportTradeRepublicCsv(userId, csv);

        // Assert
        result.Imported.Should().Be(3);
        result.SkippedDuplicates.Should().Be(0);
        result.Errors.Should().ContainSingle(e => e.Fingerprint == "66666666-6666-6666-6666-666666666666");
    }

    [Fact]
    public async Task ImportTradeRepublicCsv_MapsBuySellAndDividendRequestsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var csv = await File.ReadAllTextAsync(FixturePath);
        List<CreateTransactionRequest>? captured = null;
        autoMocker.GetMock<ITransactionService>()
            .Setup(x => x.CreateBulk(userId, It.IsAny<List<CreateTransactionRequest>>()))
            .Callback<Guid, List<CreateTransactionRequest>>((_, requests) => captured = requests)
            .ReturnsAsync(new List<Transaction>());

        // Act
        await sut.ImportTradeRepublicCsv(userId, csv);

        // Assert
        captured.Should().HaveCount(3);

        var buy = captured!.Single(r => r.ImportFingerprint == "22222222-2222-2222-2222-222222222222");
        buy.Ticker.Should().Be("WORLD");
        buy.TransactionType.Should().Be(TransactionType.Buy);
        buy.SharesQuantity.Should().Be(1.5m);
        buy.SharePrice.Should().Be(100.0000000000m);
        buy.Fees.Should().Be(0m);
        buy.ImportSource.Should().Be("TradeRepublic");

        var sell = captured.Single(r => r.ImportFingerprint == "33333333-3333-3333-3333-333333333333");
        sell.Ticker.Should().Be("WORLD");
        sell.TransactionType.Should().Be(TransactionType.Sell);
        sell.Fees.Should().Be(0.5m);

        var dividend = captured.Single(r => r.ImportFingerprint == "44444444-4444-4444-4444-444444444444");
        dividend.Ticker.Should().Be("DIVCO");
        dividend.TransactionType.Should().Be(TransactionType.Dividend);
        dividend.SharesQuantity.Should().Be(10m);
        dividend.Tax.Should().Be(1.00m);
        dividend.TotalAmount.Should().Be(4.00m);
    }

    [Fact]
    public async Task ImportTradeRepublicCsv_SkipsAlreadyImportedFingerprints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var csv = await File.ReadAllTextAsync(FixturePath);
        autoMocker.GetMock<ITransactionRepository>()
            .Setup(x => x.GetExistingImportFingerprintsAsync(userId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new HashSet<string> { "22222222-2222-2222-2222-222222222222" });

        // Act
        var result = await sut.ImportTradeRepublicCsv(userId, csv);

        // Assert
        result.SkippedDuplicates.Should().Be(1);
        result.Imported.Should().Be(2);
    }
}
