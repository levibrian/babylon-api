using System.Globalization;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Import.Services;

/// <summary>
/// Deterministic parser for Trade Republic's "Transaction export.csv". Only BUY/SELL/DIVIDEND
/// rows map to Babylon transactions; cash-only rows (interest, transfers) are not.
/// </summary>
public static class TradeRepublicCsvParser
{
    public const string Source = "TradeRepublic";

    public static IReadOnlyList<ParsedCsvRow> Parse(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        if (lines.Count == 0)
        {
            return [];
        }

        var header = SplitCsvLine(lines[0]);
        var columnIndex = header
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index);

        var rows = new List<ParsedCsvRow>();

        foreach (var line in lines.Skip(1))
        {
            var fields = SplitCsvLine(line);
            string Get(string column) => fields[columnIndex[column]];

            var transactionType = Get("type") switch
            {
                "BUY" => TransactionType.Buy,
                "SELL" => TransactionType.Sell,
                "DIVIDEND" => TransactionType.Dividend,
                _ => (TransactionType?)null
            };

            if (transactionType is null)
            {
                continue;
            }

            var date = DateOnly.Parse(Get("date"), CultureInfo.InvariantCulture);
            var isin = Get("symbol");
            var shares = ParseDecimal(Get("shares")) ?? 0m;
            var fingerprint = Get("transaction_id");

            if (transactionType == TransactionType.Dividend)
            {
                var grossAmount = ParseDecimal(Get("amount")) ?? 0m;
                var tax = Math.Abs(ParseDecimal(Get("tax")) ?? 0m);

                rows.Add(new ParsedCsvRow
                {
                    Isin = isin,
                    TransactionType = TransactionType.Dividend,
                    Date = date,
                    SharesQuantity = shares,
                    Tax = tax,
                    TotalAmount = grossAmount - tax,
                    Fingerprint = fingerprint
                });
            }
            else
            {
                var price = ParseDecimal(Get("price")) ?? 0m;
                var fee = Math.Abs(ParseDecimal(Get("fee")) ?? 0m);

                rows.Add(new ParsedCsvRow
                {
                    Isin = isin,
                    TransactionType = transactionType.Value,
                    Date = date,
                    SharesQuantity = shares,
                    SharePrice = price,
                    Fees = fee,
                    Fingerprint = fingerprint
                });
            }
        }

        return rows;
    }

    private static decimal? ParseDecimal(string value)
        => string.IsNullOrWhiteSpace(value) ? null : decimal.Parse(value, CultureInfo.InvariantCulture);

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
