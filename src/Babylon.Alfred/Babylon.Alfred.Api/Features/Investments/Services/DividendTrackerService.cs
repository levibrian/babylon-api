using Babylon.Alfred.Api.Features.Investments.Models.Responses.Dividends;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class DividendTrackerService(ITransactionRepository transactionRepository) : IDividendTrackerService
{
    private const int PastMonths = 6;
    private const int FutureMonths = 6;
    private const int RecentDividendsForDps = 3;

    public async Task<DividendTrackerResponse> GetDividendTracker(Guid userId)
    {
        var dividendTransactions = (await transactionRepository.GetDividendTransactionsByUser(userId)).ToList();

        var paid = BuildPaidMonths(dividendTransactions);
        var projected = await BuildProjectedMonths(userId, dividendTransactions);

        return new DividendTrackerResponse
        {
            Paid = paid,
            Projected = projected
        };
    }

    // ─── Paid ────────────────────────────────────────────────────────────────

    private static List<DividendMonthDto> BuildPaidMonths(List<Transaction> dividends)
    {
        var now = DateTime.UtcNow;
        var result = new List<DividendMonthDto>(PastMonths);

        for (var i = PastMonths - 1; i >= 0; i--)
        {
            var month = now.AddMonths(-i);
            var amount = dividends
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .Sum(t => t.TotalAmount);

            result.Add(new DividendMonthDto
            {
                Month = month.Month,
                Year = month.Year,
                Amount = amount,
                Label = month.ToString("MMM yyyy")
            });
        }

        return result;
    }

    // ─── Projected ───────────────────────────────────────────────────────────

    private async Task<List<DividendMonthDto>> BuildProjectedMonths(Guid userId, List<Transaction> dividends)
    {
        var now = DateTime.UtcNow;
        var projectionEnd = now.AddMonths(FutureMonths);
        var projectedAmounts = new decimal[FutureMonths];

        var bySecurityId = dividends.GroupBy(t => t.SecurityId);

        foreach (var group in bySecurityId)
        {
            var securityId = group.Key;
            var securityDividends = group.OrderBy(t => t.Date).ToList();

            // 1. Average gross DPS from the most recent dividends
            var avgDps = securityDividends
                .TakeLast(RecentDividendsForDps)
                .Average(t => t.SharePrice);

            if (avgDps <= 0)
            {
                continue;
            }

            // 2. Detect payout cadence from intervals between consecutive dividends.
            //    This is more reliable than matching calendar months: even if the user has
            //    only logged a subset of historical payouts, the gaps between the ones
            //    they did log reveal the true frequency (monthly / quarterly / semi-annual / annual).
            var cadenceDays = DetectCadenceDays(securityDividends);

            // 3. Share projection: current shares + recurring investment rate * months ahead
            var buySell = (await transactionRepository.GetBuyAndSellTransactionsByUserAndSecurity(userId, securityId)).ToList();
            var currentShares = ComputeCurrentShares(buySell);
            var recurringMonthlyShares = ComputeRecurringMonthlyRate(buySell, now);

            // 4. Walk forward from the last recorded dividend at the detected cadence,
            //    adding projected amounts for every payout date that falls in the next 6 months.
            var lastDividendDate = securityDividends.Last().Date;
            var nextDate = lastDividendDate.AddDays(cadenceDays);

            while (nextDate <= projectionEnd)
            {
                if (nextDate > now)
                {
                    // monthIndex: 0 = next calendar month, 5 = 6 months ahead
                    var monthIndex = (nextDate.Year - now.Year) * 12 + nextDate.Month - now.Month - 1;

                    if (monthIndex >= 0 && monthIndex < FutureMonths)
                    {
                        var monthsAhead = monthIndex + 1;
                        var projectedShares = Math.Max(0m, currentShares + recurringMonthlyShares * monthsAhead);
                        projectedAmounts[monthIndex] += projectedShares * avgDps;
                    }
                }

                nextDate = nextDate.AddDays(cadenceDays);
            }
        }

        var result = new List<DividendMonthDto>(FutureMonths);
        for (var i = 0; i < FutureMonths; i++)
        {
            var month = now.AddMonths(i + 1);
            result.Add(new DividendMonthDto
            {
                Month = month.Month,
                Year = month.Year,
                Amount = projectedAmounts[i],
                Label = month.ToString("MMM yyyy")
            });
        }

        return result;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the dividend payout cadence from the average interval between consecutive payments.
    /// Buckets: Monthly (~30d), Quarterly (~91d), Semi-annual (~182d), Annual (~365d).
    /// Falls back to Annual when fewer than 2 data points are available.
    /// </summary>
    private static int DetectCadenceDays(List<Transaction> dividends)
    {
        if (dividends.Count < 2)
        {
            return 365; // single data point — assume annual
        }

        var avgIntervalDays = dividends
            .Zip(dividends.Skip(1), (a, b) => (b.Date - a.Date).TotalDays)
            .Average();

        return avgIntervalDays switch
        {
            < 45 => 30,    // Monthly
            < 105 => 91,   // Quarterly
            < 200 => 182,  // Semi-annual
            _ => 365       // Annual
        };
    }

    /// <summary>
    /// Returns the total shares currently held for a security.
    /// Includes all buy and sell transactions regardless of fees.
    /// </summary>
    private static decimal ComputeCurrentShares(List<Transaction> buySell)
    {
        var shares = buySell.Sum(t => t.TransactionType == TransactionType.Buy
            ? t.SharesQuantity
            : -t.SharesQuantity);

        return Math.Max(0m, shares);
    }

    /// <summary>
    /// Returns the average monthly shares added through recurring investments over the past 6 months.
    /// Only transactions with Fees == 0 are considered recurring (scheduled DCA).
    /// Manual buy orders (Fees > 0) are opportunistic and excluded from the projection.
    /// </summary>
    private static decimal ComputeRecurringMonthlyRate(List<Transaction> buySell, DateTime now)
    {
        var windowStart = now.AddMonths(-PastMonths);

        var recurringSharesInWindow = buySell
            .Where(t => t.TransactionType == TransactionType.Buy
                     && t.Fees == 0
                     && t.Date >= windowStart)
            .Sum(t => t.SharesQuantity);

        return recurringSharesInWindow / PastMonths;
    }
}
