using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Logging;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class TransactionService(ITransactionRepository transactionRepository, ISecurityRepository securityRepository, ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<Transaction> Create(CreateTransactionRequest request)
    {
        logger.LogOperationStart("CreateTransaction", new { Ticker = request.Ticker, UserId = request.UserId });

        // Validation
        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            logger.LogValidationFailure("CreateTransaction", "Ticker cannot be null or empty", request);
            throw new ArgumentException("Ticker cannot be null or empty", nameof(request));
        }

        if (request.SharesQuantity <= 0)
        {
            logger.LogValidationFailure("CreateTransaction", "SharesQuantity must be greater than zero", request);
            throw new ArgumentException("SharesQuantity must be greater than zero", nameof(request));
        }

        if (request.TransactionType != TransactionType.Dividend && request.SharePrice <= 0)
        {
            logger.LogValidationFailure("CreateTransaction", "SharePrice must be greater than zero", request);
            throw new ArgumentException("SharePrice must be greater than zero", nameof(request));
        }

        if (request.TransactionType == TransactionType.Dividend && request.SharePrice < 0)
        {
            logger.LogValidationFailure("CreateTransaction", "SharePrice cannot be negative for dividends", request);
            throw new ArgumentException("SharePrice cannot be negative for dividends", nameof(request));
        }

        var securityFromDb = await securityRepository.GetByTickerAsync(request.Ticker);

        if (securityFromDb is null)
        {
            logger.LogBusinessRuleViolation("CreateTransaction", $"Security not found for ticker: {request.Ticker}", request);
            throw new InvalidOperationException("Security provided not found in our internal database.");
        }

        // Map to entity
        var transaction = CreateTransaction(request, securityFromDb.Id);

        // Save to database
        var result = await transactionRepository.Add(transaction);

        logger.LogOperationSuccess("CreateTransaction", new { TransactionId = result.Id, Ticker = request.Ticker });
        return result;
    }

    public async Task<IList<Transaction>> CreateBulk(List<CreateTransactionRequest> requests)
    {
        logger.LogOperationStart("CreateBulkTransactions", new { Count = requests.Count });

        // Get all unique tickers and fetch securities
        var tickers = requests.Select(r => r.Ticker).Distinct().ToList();
        var securities = await securityRepository.GetByTickersAsync(tickers);

        // Validate all tickers exist
        var missingTickers = tickers.Where(t => !securities.ContainsKey(t)).ToList();
        if (missingTickers.Any())
        {
            logger.LogBusinessRuleViolation("CreateBulkTransactions",
                $"Securities not found for tickers: {string.Join(", ", missingTickers)}",
                new { MissingTickers = missingTickers });
            throw new InvalidOperationException($"Securities not found for tickers: {string.Join(", ", missingTickers)}");
        }

        var createdTransactions = requests
            .Select(r => CreateTransaction(r, securities[r.Ticker].Id))
            .ToList();

        if (createdTransactions.Count == 0)
        {
            logger.LogInformation("CreateBulkTransactions - No transactions to create. Skipped execution");
            return new List<Transaction>();
        }

        await transactionRepository.AddBulk(createdTransactions!);

        logger.LogOperationSuccess("CreateBulkTransactions", new { Count = createdTransactions.Count });
        return createdTransactions;
    }

    public async Task<PortfolioTransactionDto> GetById(Guid id)
    {
        var transaction = new PortfolioTransactionDto();

        return transaction;
    }

    public async Task<IEnumerable<TransactionDto>> GetAllByUser(Guid? userId)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;
        logger.LogOperationStart("GetAllTransactionsByUser", new { UserId = effectiveUserId });

        var transactions = await transactionRepository.GetAllByUser(effectiveUserId);

        logger.LogInformation("Retrieved {Count} transactions from repository. Transaction types: {Types}",
            transactions.Count(),
            string.Join(", ", transactions.Select(t => t.TransactionType.ToString())));

        var transactionDtos = transactions.Select(t =>
        {
            try
            {
                var dto = new TransactionDto
                {
                    Id = t.Id,
                    Ticker = t.Security?.Ticker ?? string.Empty,
                    SecurityName = t.Security?.SecurityName ?? string.Empty,
                    SecurityType = t.Security?.SecurityType ?? SecurityType.Stock,
                    Date = t.Date,
                    SharesQuantity = t.SharesQuantity,
                    SharePrice = t.SharePrice,
                    Fees = t.Fees,
                    Tax = t.Tax,
                    TransactionType = t.TransactionType
                };
                // Access TotalAmount to ensure it's calculated (for debugging)
                var _ = dto.TotalAmount;
                return dto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error mapping transaction {TransactionId} of type {TransactionType}", t.Id, t.TransactionType);
                throw;
            }
        }).ToList();

        logger.LogOperationSuccess("GetAllTransactionsByUser", new { transactionDtos.Count, UserId = effectiveUserId });
        logger.LogInformation("Mapped {Count} transaction DTOs. Transaction types: {Types}",
            transactionDtos.Count,
            string.Join(", ", transactionDtos.Select(t => t.TransactionType.ToString())));
        return transactionDtos;
    }

    public async Task<TransactionDto> Update(Guid userId, Guid transactionId, UpdateTransactionRequest request)
    {
        logger.LogOperationStart("UpdateTransaction", new { TransactionId = transactionId, UserId = userId });

        // Get existing transaction
        var existingTransaction = await transactionRepository.GetById(transactionId, userId);
        if (existingTransaction == null)
        {
            logger.LogBusinessRuleViolation("UpdateTransaction",
                $"Transaction {transactionId} not found for user {userId}",
                new { TransactionId = transactionId, UserId = userId });
            throw new InvalidOperationException($"Transaction {transactionId} not found for user {userId}");
        }

        // Update ticker/security if provided
        if (!string.IsNullOrWhiteSpace(request.Ticker))
        {
            var securityFromDb = await securityRepository.GetByTickerAsync(request.Ticker);
            if (securityFromDb == null)
            {
                logger.LogBusinessRuleViolation("UpdateTransaction",
                    $"Security not found for ticker: {request.Ticker}",
                    request);
                throw new InvalidOperationException("Security provided not found in our internal database.");
            }
            existingTransaction.SecurityId = securityFromDb.Id;
        }

        // Update other properties if provided
        if (request.TransactionType.HasValue)
        {
            existingTransaction.TransactionType = request.TransactionType.Value;
        }

        if (request.Date.HasValue)
        {
            existingTransaction.Date = request.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        if (request.SharesQuantity.HasValue)
        {
            if (request.SharesQuantity.Value <= 0)
            {
                logger.LogValidationFailure("UpdateTransaction", "SharesQuantity must be greater than zero", request);
                throw new ArgumentException("SharesQuantity must be greater than zero", nameof(request));
            }
            existingTransaction.SharesQuantity = request.SharesQuantity.Value;
        }

        if (request.SharePrice.HasValue)
        {
            if (request.SharePrice.Value <= 0)
            {
                logger.LogValidationFailure("UpdateTransaction", "SharePrice must be greater than zero", request);
                throw new ArgumentException("SharePrice must be greater than zero", nameof(request));
            }
            existingTransaction.SharePrice = request.SharePrice.Value;
        }

        if (request.Fees.HasValue)
        {
            existingTransaction.Fees = request.Fees.Value;
        }

        if (request.Tax.HasValue)
        {
            existingTransaction.Tax = request.Tax.Value;
        }

        // Update UpdatedAt timestamp
        existingTransaction.UpdatedAt = DateTime.UtcNow;

        // Save changes
        var updatedTransaction = await transactionRepository.Update(existingTransaction);

        // Map to DTO
        var transactionDto = new TransactionDto
        {
            Id = updatedTransaction.Id,
            Ticker = updatedTransaction.Security?.Ticker ?? string.Empty,
            SecurityName = updatedTransaction.Security?.SecurityName ?? string.Empty,
            SecurityType = updatedTransaction.Security?.SecurityType ?? SecurityType.Stock,
            Date = updatedTransaction.Date,
            SharesQuantity = updatedTransaction.SharesQuantity,
            SharePrice = updatedTransaction.SharePrice,
            Fees = updatedTransaction.Fees,
            Tax = updatedTransaction.Tax,
            TransactionType = updatedTransaction.TransactionType
        };

        logger.LogOperationSuccess("UpdateTransaction", new { TransactionId = transactionId, UserId = userId });
        return transactionDto;
    }

    public async Task Delete(Guid userId, Guid transactionId)
    {
        logger.LogOperationStart("DeleteTransaction", new { TransactionId = transactionId, UserId = userId });

        await transactionRepository.Delete(transactionId, userId);

        logger.LogOperationSuccess("DeleteTransaction", new { TransactionId = transactionId, UserId = userId });
    }

    private static Transaction CreateTransaction(CreateTransactionRequest request, Guid securityId)
    {
        var transactionDate = request.Date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? DateTime.UtcNow;

        // For dividends, calculate Gross Dividend Rate from Net Amount + Tax
        decimal sharePrice;
        if (request.TransactionType == TransactionType.Dividend)
        {
            // User sends Net Amount (TotalAmount) and Tax
            // Calculate Gross Amount = Net Amount + Tax
            // Then calculate Gross Dividend Per Share = Gross Amount / SharesQuantity
            var netAmount = request.TotalAmount ?? 0;
            var grossAmount = netAmount + request.Tax;
            sharePrice = grossAmount / request.SharesQuantity;
        }
        else
        {
            sharePrice = request.SharePrice;
        }

        return new()
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
}
