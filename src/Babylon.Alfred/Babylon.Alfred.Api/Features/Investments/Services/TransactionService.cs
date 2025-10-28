using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class TransactionService(ITransactionRepository transactionRepository, ICompanyRepository companyRepository, ILogger<TransactionService> logger)
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

        var companyFromDb = await companyRepository.GetByTickerAsync(request.Ticker);

        if (companyFromDb is null)
        {
            throw new InvalidOperationException("Company provided not found in our internal database.");
        }

        // Map to entity
        var transaction = CreateTransaction(request);

        // Save to database
        return await transactionRepository.Add(transaction);
    }

    public async Task<IList<Transaction>> CreateBulk(List<CreateTransactionRequest> requests)
    {
        var createdTransactions = requests
            .Select(CreateTransaction)
            .ToList();

        if (createdTransactions.Count == 0)
        {
            logger.LogInformation("{CreateBulkAsyncMethod} - No transactions to create. Skipped execution", nameof(CreateBulk));
            return new List<Transaction>();
        }

        await transactionRepository.AddBulk(createdTransactions!);

        return createdTransactions;
    }

    private static Transaction CreateTransaction(CreateTransactionRequest request)
        => new()
        {
            Ticker = request.Ticker,
            TransactionType = request.TransactionType,
            Date = request.Date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? DateTime.UtcNow,
            SharesQuantity = request.SharesQuantity,
            SharePrice = request.SharePrice,
            Fees = request.Fees,
        };
}
