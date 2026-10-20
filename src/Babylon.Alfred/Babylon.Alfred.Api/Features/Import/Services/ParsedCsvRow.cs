using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Import.Services;

public record ParsedCsvRow
{
    public required string Isin { get; init; }
    public required TransactionType TransactionType { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal SharesQuantity { get; init; }
    public decimal SharePrice { get; init; }
    public decimal Fees { get; init; }
    public decimal Tax { get; init; }

    /// <summary>Dividend only: net amount (gross - tax).</summary>
    public decimal? TotalAmount { get; init; }

    public required string Fingerprint { get; init; }
}
