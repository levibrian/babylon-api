using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Validates transaction requests according to business rules.
/// </summary>
public static class TransactionValidator
{
    /// <summary>
    /// Validates a create transaction request.
    /// </summary>
    /// <param name="request">The transaction request to validate</param>
    /// <param name="logger">Logger instance for validation failures</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void ValidateCreateRequest(CreateTransactionRequest request, ILogger logger)
    {
        ValidateTicker(request.Ticker, logger);
        ValidateSharesQuantity(request.SharesQuantity, logger);
        ValidateSharePrice(request.SharePrice, request.TransactionType, logger);
    }

    /// <summary>
    /// Validates an update transaction request.
    /// </summary>
    /// <param name="request">The transaction request to validate</param>
    /// <param name="effectiveTransactionType">The effective transaction type (from request or existing transaction)</param>
    /// <param name="logger">Logger instance for validation failures</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void ValidateUpdateRequest(UpdateTransactionRequest request, TransactionType effectiveTransactionType, ILogger logger)
    {
        if (request.SharesQuantity.HasValue)
        {
            ValidateSharesQuantity(request.SharesQuantity.Value, logger);
        }

        if (request.SharePrice.HasValue)
        {
            // Validate SharePrice based on the effective transaction type
            ValidateSharePrice(request.SharePrice.Value, effectiveTransactionType, logger);
        }
    }

    private static void ValidateTicker(string ticker, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            logger.LogValidationFailure("TransactionValidator", ErrorMessages.TickerRequired, new { Ticker = ticker });
            throw new ArgumentException(ErrorMessages.TickerRequired, nameof(ticker));
        }
    }

    private static void ValidateSharesQuantity(decimal sharesQuantity, ILogger logger)
    {
        if (sharesQuantity <= 0)
        {
            logger.LogValidationFailure("TransactionValidator", ErrorMessages.SharesQuantityMustBePositive, new { SharesQuantity = sharesQuantity });
            throw new ArgumentException(ErrorMessages.SharesQuantityMustBePositive, nameof(sharesQuantity));
        }
    }

    private static void ValidateSharePrice(decimal sharePrice, TransactionType transactionType, ILogger logger)
    {
        // Splits have SharePrice = 0 (represents no money exchanged)
        if (transactionType == TransactionType.Split)
        {
            if (sharePrice != 0)
            {
                logger.LogValidationFailure("TransactionValidator", ErrorMessages.SharePriceMustBeZeroForSplits, new { SharePrice = sharePrice });
                throw new ArgumentException(ErrorMessages.SharePriceMustBeZeroForSplits, nameof(sharePrice));
            }
            return;
        }

        if (transactionType != TransactionType.Dividend && sharePrice <= 0)
        {
            logger.LogValidationFailure("TransactionValidator", ErrorMessages.SharePriceMustBePositive, new { SharePrice = sharePrice });
            throw new ArgumentException(ErrorMessages.SharePriceMustBePositive, nameof(sharePrice));
        }

        if (transactionType == TransactionType.Dividend && sharePrice < 0)
        {
            logger.LogValidationFailure("TransactionValidator", ErrorMessages.SharePriceCannotBeNegativeForDividends, new { SharePrice = sharePrice });
            throw new ArgumentException(ErrorMessages.SharePriceCannotBeNegativeForDividends, nameof(sharePrice));
        }
    }
}

