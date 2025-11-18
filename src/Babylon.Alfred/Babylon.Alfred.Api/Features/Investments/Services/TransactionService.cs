using Babylon.Alfred.Api.Features.Investments.Models.Requests;
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

        if (request.SharePrice <= 0)
        {
            logger.LogValidationFailure("CreateTransaction", "SharePrice must be greater than zero", request);
            throw new ArgumentException("SharePrice must be greater than zero", nameof(request));
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

    private static Transaction CreateTransaction(CreateTransactionRequest request, Guid securityId)
        => new()
        {
            SecurityId = securityId,
            TransactionType = request.TransactionType,
            Date = request.Date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? DateTime.UtcNow,
            SharesQuantity = request.SharesQuantity,
            SharePrice = request.SharePrice,
            Fees = request.Fees,
            UserId = request.UserId ?? Constants.User.RootUserId
        };
}
