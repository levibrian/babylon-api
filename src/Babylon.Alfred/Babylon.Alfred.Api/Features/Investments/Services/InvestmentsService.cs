using Babylon.Alfred.Api.Features.Investments.DTOs;
using Babylon.Alfred.Api.Features.Investments.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class InvestmentsService : IInvestmentsService
{
    private readonly List<Transaction> _transactions = new();
    private readonly List<Asset> _assets = new();
    private readonly object _lock = new();

    public async Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request)
    {
        var transaction = new Transaction
        {
            AssetSymbol = request.AssetSymbol,
            AssetName = request.AssetName,
            AssetType = request.AssetType,
            Type = request.Type,
            Date = request.Date,
            Amount = request.Amount,
            SharesQuantity = request.SharesQuantity,
            SharePrice = request.SharePrice,
            Fees = request.Fees,
            TotalAmountInvested = request.Amount + request.Fees,
            Notes = request.Notes
        };

        lock (_lock)
        {
            _transactions.Add(transaction);
        }

        // Ensure asset exists
        await EnsureAssetExistsAsync(request.AssetSymbol, request.AssetName, request.AssetType);

        return MapToTransactionResponse(transaction);
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(Guid id)
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            var transaction = _transactions.FirstOrDefault(t => t.Id == id);
            return transaction == null ? null : MapToTransactionResponse(transaction);
        }
    }

    public async Task<IEnumerable<TransactionResponse>> GetAllTransactionsAsync()
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            return _transactions
                .OrderByDescending(t => t.Date)
                .Select(MapToTransactionResponse)
                .ToList();
        }
    }

    public async Task<IEnumerable<TransactionResponse>> GetTransactionsByAssetAsync(string assetSymbol)
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            return _transactions
                .Where(t => t.AssetSymbol.Equals(assetSymbol, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Date)
                .Select(MapToTransactionResponse)
                .ToList();
        }
    }

    public async Task<IEnumerable<TransactionResponse>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            return _transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .Select(MapToTransactionResponse)
                .ToList();
        }
    }

    public async Task<TransactionResponse> UpdateTransactionAsync(Guid id, UpdateTransactionRequest request)
    {
        lock (_lock)
        {
            var transaction = _transactions.FirstOrDefault(t => t.Id == id);
            if (transaction == null)
                throw new ArgumentException($"Transaction with ID {id} not found.");

            transaction.AssetSymbol = request.AssetSymbol;
            transaction.AssetName = request.AssetName;
            transaction.AssetType = request.AssetType;
            transaction.Type = request.Type;
            transaction.Date = request.Date;
            transaction.Amount = request.Amount;
            transaction.SharesQuantity = request.SharesQuantity;
            transaction.SharePrice = request.SharePrice;
            transaction.Fees = request.Fees;
            transaction.TotalAmountInvested = request.Amount + request.Fees;
            transaction.Notes = request.Notes;
            transaction.UpdatedAt = DateTime.UtcNow;

            // Ensure asset exists
            EnsureAssetExistsAsync(request.AssetSymbol, request.AssetName, request.AssetType).Wait();

            return MapToTransactionResponse(transaction);
        }
    }

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            var transaction = _transactions.FirstOrDefault(t => t.Id == id);
            if (transaction == null)
                return false;

            _transactions.Remove(transaction);
            return true;
        }
    }

    public async Task<InvestmentSummaryResponse> GetInvestmentSummaryAsync()
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            var totalInvested = _transactions.Sum(t => t.TotalAmountInvested);
            var totalFees = _transactions.Sum(t => t.Fees);
            var totalValue = totalInvested; // For now, assume no price changes
            var totalGainLoss = totalValue - totalInvested;
            var totalGainLossPercentage = totalInvested > 0 ? (totalGainLoss / totalInvested) * 100 : 0;
            var totalTransactions = _transactions.Count;
            var uniqueAssets = _transactions.Select(t => t.AssetSymbol).Distinct().Count();
            var lastTransactionDate = _transactions.Any() ? _transactions.Max(t => t.Date) : DateTime.MinValue;

            // Calculate top holdings
            var holdings = CalculateHoldings();
            var topHoldings = holdings
                .OrderByDescending(h => h.TotalInvested)
                .Take(10)
                .Select(MapToAssetHoldingResponse)
                .ToList();

            // Calculate asset type breakdown
            var assetTypeBreakdown = _transactions
                .GroupBy(t => t.AssetType)
                .Select(g => new AssetTypeSummaryResponse
                {
                    AssetType = g.Key,
                    TotalInvested = g.Sum(t => t.TotalAmountInvested),
                    AssetCount = g.Select(t => t.AssetSymbol).Distinct().Count()
                })
                .ToList();

            // Calculate percentages for asset type breakdown
            foreach (var breakdown in assetTypeBreakdown)
            {
                breakdown.Percentage = totalInvested > 0 ? (breakdown.TotalInvested / totalInvested) * 100 : 0;
            }

            return new InvestmentSummaryResponse
            {
                TotalInvested = totalInvested,
                TotalFees = totalFees,
                TotalValue = totalValue,
                TotalGainLoss = totalGainLoss,
                TotalGainLossPercentage = totalGainLossPercentage,
                TotalTransactions = totalTransactions,
                UniqueAssets = uniqueAssets,
                LastTransactionDate = lastTransactionDate,
                TopHoldings = topHoldings,
                AssetTypeBreakdown = assetTypeBreakdown
            };
        }
    }

    public async Task<IEnumerable<AssetHoldingResponse>> GetCurrentHoldingsAsync()
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            var holdings = CalculateHoldings();
            return holdings.Select(MapToAssetHoldingResponse).ToList();
        }
    }

    public async Task<AssetHoldingResponse?> GetAssetHoldingAsync(string assetSymbol)
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            var assetTransactions = _transactions
                .Where(t => t.AssetSymbol.Equals(assetSymbol, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!assetTransactions.Any())
                return null;

            var holding = CalculateHoldingForAsset(assetSymbol, assetTransactions);
            return MapToAssetHoldingResponse(holding);
        }
    }

    public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            return _assets.ToList();
        }
    }

    public async Task<Asset?> GetAssetBySymbolAsync(string symbol)
    {
        await Task.CompletedTask; // Simulate async operation
        
        lock (_lock)
        {
            return _assets.FirstOrDefault(a => a.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<Asset> CreateAssetAsync(string symbol, string name, AssetType type, string currency = "EUR")
    {
        var asset = new Asset
        {
            Symbol = symbol,
            Name = name,
            Type = type,
            Currency = currency
        };

        lock (_lock)
        {
            _assets.Add(asset);
        }

        return asset;
    }

    private async Task EnsureAssetExistsAsync(string symbol, string name, AssetType type)
    {
        var existingAsset = await GetAssetBySymbolAsync(symbol);
        if (existingAsset == null)
        {
            await CreateAssetAsync(symbol, name, type);
        }
    }

    private List<AssetHolding> CalculateHoldings()
    {
        var holdings = new List<AssetHolding>();

        var assetGroups = _transactions
            .GroupBy(t => t.AssetSymbol)
            .ToList();

        foreach (var group in assetGroups)
        {
            var holding = CalculateHoldingForAsset(group.Key, group.ToList());
            holdings.Add(holding);
        }

        return holdings;
    }

    private AssetHolding CalculateHoldingForAsset(string assetSymbol, List<Transaction> transactions)
    {
        var buyTransactions = transactions.Where(t => t.Type == TransactionType.Buy).ToList();
        var sellTransactions = transactions.Where(t => t.Type == TransactionType.Sell).ToList();

        var totalSharesBought = buyTransactions.Sum(t => t.SharesQuantity);
        var totalSharesSold = sellTransactions.Sum(t => t.SharesQuantity);
        var totalShares = totalSharesBought - totalSharesSold;

        var totalInvested = buyTransactions.Sum(t => t.TotalAmountInvested) - sellTransactions.Sum(t => t.TotalAmountInvested);
        var averagePrice = totalShares > 0 ? totalInvested / totalShares : 0;

        var currentValue = totalShares * averagePrice; // For now, assume no price changes
        var gainLoss = currentValue - totalInvested;
        var gainLossPercentage = totalInvested > 0 ? (gainLoss / totalInvested) * 100 : 0;

        return new AssetHolding
        {
            AssetSymbol = assetSymbol,
            AssetName = transactions.First().AssetName,
            AssetType = transactions.First().AssetType,
            TotalShares = totalShares,
            AveragePrice = averagePrice,
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            GainLoss = gainLoss,
            GainLossPercentage = gainLossPercentage
        };
    }

    private static TransactionResponse MapToTransactionResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            AssetSymbol = transaction.AssetSymbol,
            AssetName = transaction.AssetName,
            AssetType = transaction.AssetType,
            Type = transaction.Type,
            Date = transaction.Date,
            Amount = transaction.Amount,
            SharesQuantity = transaction.SharesQuantity,
            SharePrice = transaction.SharePrice,
            Fees = transaction.Fees,
            TotalAmountInvested = transaction.TotalAmountInvested,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    private static AssetHoldingResponse MapToAssetHoldingResponse(AssetHolding holding)
    {
        return new AssetHoldingResponse
        {
            AssetSymbol = holding.AssetSymbol,
            AssetName = holding.AssetName,
            AssetType = holding.AssetType,
            TotalShares = holding.TotalShares,
            AveragePrice = holding.AveragePrice,
            TotalInvested = holding.TotalInvested,
            CurrentValue = holding.CurrentValue,
            GainLoss = holding.GainLoss,
            GainLossPercentage = holding.GainLossPercentage
        };
    }
}

