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
    ICashBalanceService cashBalanceService,
    ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<Transaction> Create(Guid userId, CreateTransactionRequest request)
    {
        logger.LogOperationStart("CreateTransaction", new { Ticker = request.Ticker, UserId = userId });

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

        var transaction = CreateTransactionEntity(userId, request, security.Id);

        await ValidateInventoryAsync(userId, [transaction.SecurityId], [transaction]);

        var result = await transactionRepository.Add(transaction);

        if (ShouldRecalculateRealizedPnL(result.TransactionType))
        {
            var recalculatedResults = await RecalculateRealizedPnLForSecuritiesAsync(
                userId,
                [result.SecurityId],
                [result]);
            ApplyRealizedPnLResults([result], recalculatedResults);
        }

        // Update cash balance
        await cashBalanceService.ProcessTransactionAsync(userId, result.TransactionType, result.TotalAmount);

        logger.LogOperationSuccess("CreateTransaction", new { TransactionId = result.Id, Ticker = request.Ticker });
        return result;
    }

    public async Task<IList<Transaction>> CreateBulk(Guid userId, List<CreateTransactionRequest> requests)
    {
        logger.LogOperationStart("CreateBulkTransactions", new { Count = requests.Count, UserId = userId });

        var tickers = requests.Select(r => r.Ticker).Distinct().ToList();
        var securities = await SecurityValidator.ValidateAndGetSecuritiesAsync(tickers, securityRepository, logger);

        var createdTransactions = requests
            .Select(r => CreateTransactionEntity(userId, r, securities[r.Ticker].Id))
            .ToList();

        if (createdTransactions.Count == 0)
        {
            logger.LogInformation("CreateBulkTransactions - No transactions to create. Skipped execution");
            return new List<Transaction>();
        }

        var baseTimeOfDay = DateTime.UtcNow.TimeOfDay;
        for (var index = 0; index < createdTransactions.Count; index++)
        {
            var transaction = createdTransactions[index];
            var createdAt = transaction.Date.Date.Add(baseTimeOfDay).AddTicks(index);
            transaction.CreatedAt = createdAt;
        }

        var securityIdsToValidate = createdTransactions.Select(t => t.SecurityId).Distinct();
        await ValidateInventoryAsync(userId, securityIdsToValidate, createdTransactions);

        await transactionRepository.AddBulk([.. createdTransactions.Cast<Transaction?>()]);

        var securitiesToRecalculate = createdTransactions
            .Where(t => ShouldRecalculateRealizedPnL(t.TransactionType))
            .Select(t => t.SecurityId)
            .Distinct()
            .ToList();

        if (securitiesToRecalculate.Count > 0)
        {
            var recalculatedResults = await RecalculateRealizedPnLForSecuritiesAsync(
                userId,
                securitiesToRecalculate,
                createdTransactions);
            ApplyRealizedPnLResults(createdTransactions, recalculatedResults);
        }

        // Update cash balance for each transaction
        foreach (var transaction in createdTransactions)
        {
            await cashBalanceService.ProcessTransactionAsync(userId, transaction.TransactionType, transaction.TotalAmount);
        }

        logger.LogOperationSuccess("CreateBulkTransactions", new { createdTransactions.Count });
        return createdTransactions;
    }

    public Task<PortfolioTransactionDto> GetById(Guid id)
    {
        // TODO: Implement GetById functionality
        logger.LogWarning("GetById called but not implemented for transaction {TransactionId}", id);
        return Task.FromResult(new PortfolioTransactionDto());
    }

    public async Task<IEnumerable<TransactionDto>> GetAllByUser(Guid userId)
    {
        var effectiveUserId = userId;
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

        // Store original values for cash balance reversal and PnL recalculation
        var originalType = existingTransaction.TransactionType;
        var originalAmount = existingTransaction.TotalAmount;
        var originalSecurityId = existingTransaction.SecurityId;

        // Determine effective transaction type for validation (use request value if provided, otherwise existing)
        var effectiveTransactionType = request.TransactionType ?? existingTransaction.TransactionType;
        TransactionValidator.ValidateUpdateRequest(request, effectiveTransactionType, logger);
        await UpdateTransactionPropertiesAsync(existingTransaction, request, securityRepository, logger);

        if (request.Date.HasValue)
        {
            var timeOfDay = existingTransaction.CreatedAt.TimeOfDay;
            existingTransaction.CreatedAt = existingTransaction.Date.Date.Add(timeOfDay);
        }

        var securityIdsToValidate = new HashSet<Guid> { originalSecurityId, existingTransaction.SecurityId };
        await ValidateInventoryAsync(userId, securityIdsToValidate, [existingTransaction], [existingTransaction.Id]);

        existingTransaction.UpdatedAt = DateTime.UtcNow;
        var updatedTransaction = await transactionRepository.Update(existingTransaction);

        if (ShouldRecalculateRealizedPnL(originalType) || ShouldRecalculateRealizedPnL(updatedTransaction.TransactionType))
        {
            var securitiesToRecalculate = new HashSet<Guid>();
            if (ShouldRecalculateRealizedPnL(originalType))
            {
                securitiesToRecalculate.Add(originalSecurityId);
            }

            if (ShouldRecalculateRealizedPnL(updatedTransaction.TransactionType))
            {
                securitiesToRecalculate.Add(updatedTransaction.SecurityId);
            }

            if (securitiesToRecalculate.Count > 0)
            {
                var recalculatedResults = await RecalculateRealizedPnLForSecuritiesAsync(
                    userId,
                    securitiesToRecalculate.ToList(),
                    [updatedTransaction]);
                ApplyRealizedPnLResults([updatedTransaction], recalculatedResults);
            }
        }

        // Revert old cash impact and apply new one
        await cashBalanceService.RevertTransactionAsync(userId, originalType, originalAmount);
        await cashBalanceService.ProcessTransactionAsync(userId, updatedTransaction.TransactionType, updatedTransaction.TotalAmount);

        logger.LogOperationSuccess("UpdateTransaction", new { TransactionId = transactionId, UserId = userId });
        return TransactionMapper.ToDto(updatedTransaction);
    }

    public async Task Delete(Guid userId, Guid transactionId)
    {
        logger.LogOperationStart("DeleteTransaction", new { TransactionId = transactionId, UserId = userId });

        var existingTransaction = await transactionRepository.GetById(transactionId, userId);
        if (existingTransaction == null)
        {
            logger.LogBusinessRuleViolation("DeleteTransaction",
                string.Format(ErrorMessages.TransactionNotFound, transactionId, userId),
                new { TransactionId = transactionId, UserId = userId });
            throw new InvalidOperationException(
                string.Format(ErrorMessages.TransactionNotFound, transactionId, userId));
        }

        // Validate that deleting this transaction doesn't cause overselling in the future
        await ValidateInventoryAsync(userId, [existingTransaction.SecurityId], [], [transactionId]);

        var deletedTransaction = await transactionRepository.Delete(transactionId, userId);

        if (ShouldRecalculateRealizedPnL(deletedTransaction.TransactionType))
        {
            await RecalculateRealizedPnLForSecuritiesAsync(userId, [deletedTransaction.SecurityId], []);
        }

        // Revert cash impact
        await cashBalanceService.RevertTransactionAsync(userId, deletedTransaction.TransactionType, deletedTransaction.TotalAmount);

        logger.LogOperationSuccess("DeleteTransaction", new { TransactionId = transactionId, UserId = userId });
    }

    private static Transaction CreateTransactionEntity(Guid userId, CreateTransactionRequest request, Guid securityId)
    {
        var transactionDate = request.Date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? DateTime.UtcNow;
        var sharePrice = request.TransactionType == TransactionType.Dividend
            ? DividendCalculator.CalculateSharePriceForDividend(request)
            : request.SharePrice;

        var createdAt = transactionDate.Date.Add(DateTime.UtcNow.TimeOfDay);
        return new Transaction
        {
            Id = Guid.NewGuid(),
            SecurityId = securityId,
            TransactionType = request.TransactionType,
            Date = transactionDate,
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow,
            SharesQuantity = request.SharesQuantity,
            SharePrice = sharePrice,
            Fees = request.Fees,
            Tax = request.Tax,
            UserId = userId
        };
    }

    private static bool ShouldRecalculateRealizedPnL(TransactionType transactionType)
    {
        return transactionType is TransactionType.Buy or TransactionType.Sell or TransactionType.Split;
    }

    private static PortfolioTransactionDto ToPortfolioTransactionDto(Transaction transaction)
    {
        return new PortfolioTransactionDto
        {
            Id = transaction.Id,
            TransactionType = transaction.TransactionType,
            Date = transaction.Date,
            UpdatedAt = transaction.UpdatedAt,
            CreatedAt = transaction.CreatedAt,
            SharesQuantity = transaction.SharesQuantity,
            SharePrice = transaction.SharePrice,
            Fees = transaction.Fees,
            Tax = transaction.Tax,
            RealizedPnL = transaction.RealizedPnL,
            RealizedPnLPct = transaction.RealizedPnLPct
        };
    }

    private static void ApplyRealizedPnLResults(
        IEnumerable<Transaction> transactions,
        IReadOnlyDictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)> recalculatedResults)
    {
        foreach (var transaction in transactions)
        {
            if (recalculatedResults.TryGetValue(transaction.Id, out var values))
            {
                transaction.RealizedPnL = values.RealizedPnL;
                transaction.RealizedPnLPct = values.RealizedPnLPct;
            }
        }
    }

    private async Task<IReadOnlyDictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)>>
        RecalculateRealizedPnLForSecuritiesAsync(
            Guid userId,
            IReadOnlyCollection<Guid> securityIds,
            IEnumerable<Transaction>? overrideTransactions)
    {
        if (securityIds.Count == 0)
        {
            return new Dictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)>();
        }

        var allTransactions = (await transactionRepository.GetAllByUser(userId))?.ToList()
            ?? new List<Transaction>();
        var transactionsById = allTransactions.ToDictionary(t => t.Id, t => t);

        if (overrideTransactions != null)
        {
            foreach (var transaction in overrideTransactions)
            {
                transactionsById[transaction.Id] = transaction;
            }
        }

        var recalculatedResults = new Dictionary<Guid, (decimal? RealizedPnL, decimal? RealizedPnLPct)>();

        foreach (var securityId in securityIds.Distinct())
        {
            var transactionsForSecurity = transactionsById.Values
                .Where(t => t.SecurityId == securityId)
                .ToList();

            if (transactionsForSecurity.Count == 0)
            {
                continue;
            }

            var portfolioTransactions = transactionsForSecurity
                .Select(ToPortfolioTransactionDto)
                .ToList();

            var results = RealizedPnLCalculator.CalculateRealizedPnLByTransactionId(portfolioTransactions);

            foreach (var transaction in transactionsForSecurity)
            {
                if (!results.TryGetValue(transaction.Id, out var values))
                {
                    continue;
                }

                recalculatedResults[transaction.Id] = values;

                if (transaction.RealizedPnL != values.RealizedPnL ||
                    transaction.RealizedPnLPct != values.RealizedPnLPct)
                {
                    transaction.RealizedPnL = values.RealizedPnL;
                    transaction.RealizedPnLPct = values.RealizedPnLPct;
                    await transactionRepository.Update(transaction);
                }
            }
        }

        return recalculatedResults;
    }

    private async Task ValidateInventoryAsync(
        Guid userId,
        IEnumerable<Guid> securityIdsToValidate,
        IEnumerable<Transaction>? overrideTransactions = null,
        IEnumerable<Guid>? excludeIds = null)
    {
        var allTransactions = (await transactionRepository.GetAllByUser(userId))?.ToList()
            ?? new List<Transaction>();

        var transactionsById = allTransactions.ToDictionary(t => t.Id, t => t);

        if (excludeIds != null)
        {
            foreach (var id in excludeIds)
            {
                transactionsById.Remove(id);
            }
        }

        if (overrideTransactions != null)
        {
            foreach (var transaction in overrideTransactions)
            {
                transactionsById[transaction.Id] = transaction;
            }
        }

        foreach (var securityId in securityIdsToValidate.Distinct())
        {
            var securityTransactions = transactionsById.Values
                .Where(t => t.SecurityId == securityId)
                .ToList();

            var sharesBeforeById = RealizedPnLCalculator.CalculateAvailableSharesBeforeTransaction(
                securityTransactions.Select(ToPortfolioTransactionDto));

            foreach (var trans in securityTransactions.Where(t => t.TransactionType == TransactionType.Sell))
            {
                if (!sharesBeforeById.TryGetValue(trans.Id, out var availableShares) ||
                    availableShares < trans.SharesQuantity)
                {
                    logger.LogBusinessRuleViolation(
                        "TransactionService",
                        ErrorMessages.InsufficientSharesToSell,
                        new
                        {
                            UserId = userId,
                            SecurityId = securityId,
                            AvailableShares = availableShares,
                            RequestedShares = trans.SharesQuantity,
                            TransactionId = trans.Id
                        });
                    throw new InvalidOperationException(ErrorMessages.InsufficientSharesToSell);
                }
            }
        }
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
