using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Shared chronological ordering for FIFO transaction processing.
/// Splits process first (take effect at market open), then dividends,
/// then buys (at post-split prices), then sells (post-split quantities).
/// </summary>
internal static class TransactionOrdering
{
    public static int GetSortOrder(TransactionType type) => type switch
    {
        TransactionType.Split => 0,
        TransactionType.Dividend => 1,
        TransactionType.Buy => 2,
        TransactionType.Sell => 3,
        _ => 4
    };
}
