using Babylon.Alfred.Api.Features.Investments.Models.Requests;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Handles dividend-specific calculations.
/// </summary>
public static class DividendCalculator
{
    /// <summary>
    /// Calculates the gross dividend per share from net amount and tax.
    /// </summary>
    /// <param name="netAmount">Net dividend amount (after tax)</param>
    /// <param name="tax">Withholding tax amount</param>
    /// <param name="sharesQuantity">Number of shares</param>
    /// <returns>Gross dividend per share</returns>
    public static decimal CalculateGrossDividendPerShare(decimal netAmount, decimal tax, decimal sharesQuantity)
    {
        if (sharesQuantity <= 0)
        {
            throw new ArgumentException("SharesQuantity must be greater than zero", nameof(sharesQuantity));
        }

        var grossAmount = netAmount + tax;
        return grossAmount / sharesQuantity;
    }

    /// <summary>
    /// Calculates the share price for a dividend transaction.
    /// For dividends, this represents the gross dividend per share.
    /// </summary>
    /// <param name="request">The transaction request</param>
    /// <returns>The calculated share price (gross dividend per share)</returns>
    public static decimal CalculateSharePriceForDividend(CreateTransactionRequest request)
    {
        var netAmount = request.TotalAmount ?? 0;
        return CalculateGrossDividendPerShare(netAmount, request.Tax, request.SharesQuantity);
    }
}

