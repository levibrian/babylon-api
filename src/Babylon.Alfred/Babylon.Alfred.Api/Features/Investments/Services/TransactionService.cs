using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Features.Investments.Repositories;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICompanyRepository _companyRepository;

    public TransactionService(ITransactionRepository transactionRepository, ICompanyRepository companyRepository)
    {
        _transactionRepository = transactionRepository;
        _companyRepository = companyRepository;
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

        // If CompanyName is not provided, try to look it up from the companies table
        string? companyName = request.CompanyName;
        if (string.IsNullOrWhiteSpace(companyName))
        {
            var company = await _companyRepository.GetByTickerAsync(request.Ticker);
            if (company != null)
            {
                companyName = company.CompanyName;
            }
        }
        else
        {
            // If a company name was provided, update/insert it in the companies table
            await _companyRepository.AddOrUpdateAsync(new Company
            {
                Ticker = request.Ticker,
                CompanyName = companyName
            });
        }

        // Map to entity
        var transaction = new Transaction
        {
            Ticker = request.Ticker,
            CompanyName = companyName,
            TransactionType = request.TransactionType,
            Date = request.Date ?? DateTime.UtcNow,
            SharesQuantity = request.SharesQuantity,
            SharePrice = request.SharePrice,
            Fees = request.Fees,
        };

        // Save to database
        return await _transactionRepository.AddAsync(transaction);
    }

    public async Task<IList<Transaction>> CreateBulkAsync(List<CreateTransactionRequest> requests)
    {
        var createdTransactions = new List<Transaction>();

        foreach (var request in requests)
        {
            var transaction = await CreateAsync(request);
            createdTransactions.Add(transaction);
        }

        return createdTransactions;
    }
}

