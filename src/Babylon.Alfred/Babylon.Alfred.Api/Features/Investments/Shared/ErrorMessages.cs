namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Centralized error messages for transaction-related operations.
/// </summary>
public static class ErrorMessages
{
    public const string TickerRequired = "Ticker cannot be null or empty";
    public const string SharesQuantityMustBePositive = "SharesQuantity must be greater than zero";
    public const string SharePriceMustBePositive = "SharePrice must be greater than zero";
    public const string SharePriceCannotBeNegativeForDividends = "SharePrice cannot be negative for dividends";
    public const string SharePriceMustBeZeroForSplits = "SharePrice must be zero for stock splits";
    public const string SecurityNotFound = "Security provided not found in our internal database.";
    public const string TransactionNotFound = "Transaction {0} not found for user {1}";
    public const string SecuritiesNotFoundForTickers = "Securities not found for tickers: {0}";
}

