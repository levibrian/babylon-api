using Babylon.Alfred.Api.Features.Import.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;

namespace Babylon.Alfred.Api.Tests.Features.Import;

public class TradeRepublicCsvParserTests
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "Features", "Import", "Fixtures", "trade-republic-sample.csv");

    [Fact]
    public void Parse_SkipsNonTradingCashRows()
    {
        var csv = File.ReadAllText(FixturePath);

        var rows = TradeRepublicCsvParser.Parse(csv);

        rows.Should().NotContain(r => r.Fingerprint == "11111111-1111-1111-1111-111111111111"); // TRANSFER_INBOUND
        rows.Should().NotContain(r => r.Fingerprint == "55555555-5555-5555-5555-555555555555"); // INTEREST_PAYMENT
    }

    [Fact]
    public void Parse_MapsBuyRow()
    {
        var csv = File.ReadAllText(FixturePath);

        var rows = TradeRepublicCsvParser.Parse(csv);

        var buy = rows.Single(r => r.Fingerprint == "22222222-2222-2222-2222-222222222222");
        buy.TransactionType.Should().Be(TransactionType.Buy);
        buy.Isin.Should().Be("XX0000000001");
        buy.Date.Should().Be(new DateOnly(2026, 1, 6));
        buy.SharesQuantity.Should().Be(1.5m);
        buy.SharePrice.Should().Be(100.0000000000m);
        buy.Fees.Should().Be(0m);
        buy.Tax.Should().Be(0m);
    }

    [Fact]
    public void Parse_MapsSellRowWithFee()
    {
        var csv = File.ReadAllText(FixturePath);

        var rows = TradeRepublicCsvParser.Parse(csv);

        var sell = rows.Single(r => r.Fingerprint == "33333333-3333-3333-3333-333333333333");
        sell.TransactionType.Should().Be(TransactionType.Sell);
        sell.SharesQuantity.Should().Be(0.5m);
        sell.SharePrice.Should().Be(110.0000000000m);
        sell.Fees.Should().Be(0.5m);
    }

    [Fact]
    public void Parse_MapsDividendRow_NetAmountIsGrossMinusTax()
    {
        var csv = File.ReadAllText(FixturePath);

        var rows = TradeRepublicCsvParser.Parse(csv);

        var dividend = rows.Single(r => r.Fingerprint == "44444444-4444-4444-4444-444444444444");
        dividend.TransactionType.Should().Be(TransactionType.Dividend);
        dividend.Isin.Should().Be("XX0000000002");
        dividend.SharesQuantity.Should().Be(10m);
        dividend.Tax.Should().Be(1.00m);
        dividend.TotalAmount.Should().Be(4.00m); // gross 5.00 - tax 1.00
    }

    [Fact]
    public void Parse_ReturnsRowsInFileOrder()
    {
        var csv = File.ReadAllText(FixturePath);

        var rows = TradeRepublicCsvParser.Parse(csv);

        rows.Select(r => r.Fingerprint).Should().ContainInOrder(
            "22222222-2222-2222-2222-222222222222",
            "33333333-3333-3333-3333-333333333333",
            "44444444-4444-4444-4444-444444444444",
            "66666666-6666-6666-6666-666666666666");
    }
}
