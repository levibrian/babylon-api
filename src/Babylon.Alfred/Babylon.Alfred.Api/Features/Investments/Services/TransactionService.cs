using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class TransactionService(ITransactionRepository transactionRepository, ISecurityRepository securityRepository, ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<Transaction> Create(CreateTransactionRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker cannot be null or empty", nameof(request));
        }

        if (request.SharesQuantity <= 0)
        {
            throw new ArgumentException("SharesQuantity must be greater than zero", nameof(request));
        }

        if (request.SharePrice <= 0)
        {
            throw new ArgumentException("SharePrice must be greater than zero", nameof(request));
        }

        var securityFromDb = await securityRepository.GetByTickerAsync(request.Ticker);

        if (securityFromDb is null)
        {
            throw new InvalidOperationException("Security provided not found in our internal database.");
        }

        // Map to entity
        var transaction = CreateTransaction(request, securityFromDb.Id);

        // Save to database
        return await transactionRepository.Add(transaction);
    }

    public async Task<IList<Transaction>> CreateBulk(List<CreateTransactionRequest> requests)
    {
        // Get all unique tickers and fetch securities
        var tickers = requests.Select(r => r.Ticker).Distinct().ToList();
        var securities = await securityRepository.GetByTickersAsync(tickers);

        // Validate all tickers exist
        var missingTickers = tickers.Where(t => !securities.ContainsKey(t)).ToList();
        if (missingTickers.Any())
        {
            throw new InvalidOperationException($"Securities not found for tickers: {string.Join(", ", missingTickers)}");
        }

        var createdTransactions = requests
            .Select(r => CreateTransaction(r, securities[r.Ticker].Id))
            .ToList();

        if (createdTransactions.Count == 0)
        {
            logger.LogInformation("{CreateBulkAsyncMethod} - No transactions to create. Skipped execution", nameof(CreateBulk));
            return new List<Transaction>();
        }

        await transactionRepository.AddBulk(createdTransactions!);

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
