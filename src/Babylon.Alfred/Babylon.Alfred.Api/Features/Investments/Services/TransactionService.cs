using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Features.Investments.Repositories;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IList<Transaction>> GetAllTransactionsAsync()
    {
        var transactions = await _transactionRepository.GetAllAsync();
        return transactions.ToList();
    }

    public async Task<Transaction> CreateAsync(CreateTransactionRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker cannot be null or empty", nameof(request.Ticker));
        }

        if (request.SharesQuantity <= 0)
        {
            throw new ArgumentException("SharesQuantity must be greater than zero", nameof(request.SharesQuantity));
        }

        if (request.SharePrice <= 0)
        {
            throw new ArgumentException("SharePrice must be greater than zero", nameof(request.SharePrice));
        }

        // Map to entity
        var transaction = new Transaction
        {
            Ticker = request.Ticker,
            TransactionType = request.TransactionType,
            Date = request.Date ?? DateTime.UtcNow,
            SharesQuantity = request.SharesQuantity,
            SharePrice = request.SharePrice,
            Fees = request.Fees,
        };

        // Save to database
        return await _transactionRepository.AddAsync(transaction);
    }
}

