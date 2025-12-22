using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Features.Investments.Shared;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;
using Babylon.Alfred.Api.Infrastructure.YahooFinance.Services;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class TransactionService(
    ITransactionRepository transactionRepository,
    ISecurityRepository securityRepository,
    IYahooMarketDataService yahooMarketDataService,
    ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<Transaction> Create(CreateTransactionRequest request)
    {
        logger.LogOperationStart("CreateTransaction", new { Ticker = request.Ticker, UserId = request.UserId });

        TransactionValidator.ValidateCreateRequest(request, logger);
        
        // Check if security exists locally
        var security = await securityRepository.GetByTickerAsync(request.Ticker);
        
        if (security == null)
        {
            logger.LogInformation("Security not found locally for ticker {Ticker}. Searching Yahoo Finance...", request.Ticker);
            
            // Search in Yahoo Finance
            var searchResults = await yahooMarketDataService.SearchAsync(request.Ticker);
            
            // Find an exact match or a very close match (case-insensitive)
            var match = searchResults.FirstOrDefault(s => string.Equals(s.Symbol, request.Ticker, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                logger.LogBusinessRuleViolation("CreateTransaction", 
                    $"Security not found locally and not found in Yahoo Finance for ticker: {request.Ticker}", 
                    new { request.Ticker });
                throw new InvalidOperationException(ErrorMessages.SecurityNotFound);
            }

            // Map and create new security
            security = new Security
            {
                Id = Guid.NewGuid(),
                Ticker = match.Symbol.ToUpperInvariant(), // Standardize ticker casing
                SecurityName = !string.IsNullOrWhiteSpace(match.LongName) ? match.LongName : match.ShortName,
                SecurityType = MapYahooQuoteTypeToSecurityType(match.QuoteType),
                Exchange = match.Exchange,
                Sector = match.Sector,
                Industry = match.Industry,
                LastUpdated = DateTime.UtcNow
            };

            security = await securityRepository.AddOrUpdateAsync(security);
            logger.LogInformation("Created new security {Ticker} from Yahoo Finance data", security.Ticker);
        }

        var transaction = CreateTransactionEntity(request, security.Id);
        var result = await transactionRepository.Add(transaction);

        logger.LogOperationSuccess("CreateTransaction", new { TransactionId = result.Id, Ticker = request.Ticker });
        return result;
    }

    public async Task<IList<Transaction>> CreateBulk(List<CreateTransactionRequest> requests)
    {
        logger.LogOperationStart("CreateBulkTransactions", new { Count = requests.Count });

        var tickers = requests.Select(r => r.Ticker).Distinct().ToList();
        var securities = await SecurityValidator.ValidateAndGetSecuritiesAsync(tickers, securityRepository, logger);

        var createdTransactions = requests
            .Select(r => CreateTransactionEntity(r, securities[r.Ticker].Id))
            .ToList();

        if (createdTransactions.Count == 0)
        {
            logger.LogInformation("CreateBulkTransactions - No transactions to create. Skipped execution");
            return new List<Transaction>();
        }

        await transactionRepository.AddBulk([.. createdTransactions.Cast<Transaction?>()]);
        logger.LogOperationSuccess("CreateBulkTransactions", new { createdTransactions.Count });
        return createdTransactions;
    }

    public Task<PortfolioTransactionDto> GetById(Guid id)
    {
        // TODO: Implement GetById functionality
        logger.LogWarning("GetById called but not implemented for transaction {TransactionId}", id);
        return Task.FromResult(new PortfolioTransactionDto());
    }

    public async Task<IEnumerable<TransactionDto>> GetAllByUser(Guid? userId)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;
        logger.LogOperationStart("GetAllTransactionsByUser", new { UserId = effectiveUserId });

        var transactions = await transactionRepository.GetAllByUser(effectiveUserId);
        var transactionDtos = TransactionMapper.ToDtoCollection(transactions).ToList();

        logger.LogOperationSuccess("GetAllTransactionsByUser", new { transactionDtos.Count, UserId = effectiveUserId });
        return transactionDtos;
    }

    public async Task<TransactionDto> Update(Guid userId, Guid transactionId, UpdateTransactionRequest request)
    {
        logger.LogOperationStart("UpdateTransaction", new { TransactionId = transactionId, UserId = userId });

        var existingTransaction = await transactionRepository.GetById(transactionId, userId);
        if (existingTransaction == null)
        {
            logger.LogBusinessRuleViolation("UpdateTransaction",
                string.Format(ErrorMessages.TransactionNotFound, transactionId, userId),
                new { TransactionId = transactionId, UserId = userId });
            throw new InvalidOperationException(
                string.Format(ErrorMessages.TransactionNotFound, transactionId, userId));
        }

        // Determine effective transaction type for validation (use request value if provided, otherwise existing)
        var effectiveTransactionType = request.TransactionType ?? existingTransaction.TransactionType;
        TransactionValidator.ValidateUpdateRequest(request, effectiveTransactionType, logger);
        await UpdateTransactionPropertiesAsync(existingTransaction, request, securityRepository, logger);

        existingTransaction.UpdatedAt = DateTime.UtcNow;
        var updatedTransaction = await transactionRepository.Update(existingTransaction);

        logger.LogOperationSuccess("UpdateTransaction", new { TransactionId = transactionId, UserId = userId });
        return TransactionMapper.ToDto(updatedTransaction);
    }

    public async Task Delete(Guid userId, Guid transactionId)
    {
        logger.LogOperationStart("DeleteTransaction", new { TransactionId = transactionId, UserId = userId });
        await transactionRepository.Delete(transactionId, userId);
        logger.LogOperationSuccess("DeleteTransaction", new { TransactionId = transactionId, UserId = userId });
    }

    private static Transaction CreateTransactionEntity(CreateTransactionRequest request, Guid securityId)
    {
        var transactionDate = request.Date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? DateTime.UtcNow;
        var sharePrice = request.TransactionType == TransactionType.Dividend
            ? DividendCalculator.CalculateSharePriceForDividend(request)
            : request.SharePrice;

        return new Transaction
        {
            SecurityId = securityId,
            TransactionType = request.TransactionType,
            Date = transactionDate,
            UpdatedAt = DateTime.UtcNow,
            SharesQuantity = request.SharesQuantity,
            SharePrice = sharePrice,
            Fees = request.Fees,
            Tax = request.Tax,
            UserId = request.UserId ?? Constants.User.RootUserId
        };
    }

    private static async Task UpdateTransactionPropertiesAsync(
        Transaction transaction,
        UpdateTransactionRequest request,
        ISecurityRepository securityRepository,
        ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(request.Ticker))
        {
            var security = await SecurityValidator.ValidateAndGetSecurityAsync(request.Ticker, securityRepository, logger);
            transaction.SecurityId = security.Id;
        }

        // Update transaction type first, as it affects validation of other fields
        if (request.TransactionType.HasValue)
        {
            transaction.TransactionType = request.TransactionType.Value;
        }

        // Determine effective transaction type (use updated value if changed, otherwise existing)
        var effectiveTransactionType = request.TransactionType ?? transaction.TransactionType;

        if (request.Date.HasValue)
        {
            transaction.Date = request.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        if (request.SharesQuantity.HasValue)
        {
            transaction.SharesQuantity = request.SharesQuantity.Value;
        }

        if (request.SharePrice.HasValue)
        {
            // For Split transactions, SharePrice must be 0
            if (effectiveTransactionType == TransactionType.Split)
            {
                if (request.SharePrice.Value != 0)
                {
                    logger.LogValidationFailure("UpdateTransactionPropertiesAsync", ErrorMessages.SharePriceMustBeZeroForSplits, new { SharePrice = request.SharePrice.Value });
                    throw new ArgumentException(ErrorMessages.SharePriceMustBeZeroForSplits, nameof(request.SharePrice));
                }
            }
            transaction.SharePrice = request.SharePrice.Value;
        }
        else if (effectiveTransactionType == TransactionType.Split && transaction.SharePrice != 0)
        {
            // If changing to Split type but SharePrice not provided, ensure it's set to 0
            transaction.SharePrice = 0;
        }

        if (request.Fees.HasValue)
        {
            transaction.Fees = request.Fees.Value;
        }

        if (request.Tax.HasValue)
        {
            transaction.Tax = request.Tax.Value;
        }
    }
    private static SecurityType MapYahooQuoteTypeToSecurityType(string quoteType)
    {
        return quoteType.ToUpperInvariant() switch
        {
            "EQUITY" => SecurityType.Stock,
            "ETF" => SecurityType.ETF,
            "MUTUALFUND" => SecurityType.MutualFund,
            "CRYPTOCURRENCY" => SecurityType.Crypto,
            "FUTURE" => SecurityType.Commodity,
            "OPTION" => SecurityType.Options,
            _ => SecurityType.Stock // Default fallback
        };
    }
}
