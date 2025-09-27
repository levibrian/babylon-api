using Babylon.Alfred.Api.Features.Investments.DTOs;
using Babylon.Alfred.Api.Features.Investments.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface IInvestmentsService
{
    // Transaction operations
    Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request);
    Task<TransactionResponse?> GetTransactionByIdAsync(Guid id);
    Task<IEnumerable<TransactionResponse>> GetAllTransactionsAsync();
    Task<IEnumerable<TransactionResponse>> GetTransactionsByAssetAsync(string assetSymbol);
    Task<IEnumerable<TransactionResponse>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<TransactionResponse> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request);
    Task<bool> DeleteTransactionAsync(Guid id);
    
    // Investment summary operations
    Task<InvestmentSummaryResponse> GetInvestmentSummaryAsync();
    Task<IEnumerable<AssetHoldingResponse>> GetCurrentHoldingsAsync();
    Task<AssetHoldingResponse?> GetAssetHoldingAsync(string assetSymbol);
    
    // Asset operations
    Task<IEnumerable<Asset>> GetAllAssetsAsync();
    Task<Asset?> GetAssetBySymbolAsync(string symbol);
    Task<Asset> CreateAssetAsync(string symbol, string name, AssetType type, string currency = "EUR");
}

