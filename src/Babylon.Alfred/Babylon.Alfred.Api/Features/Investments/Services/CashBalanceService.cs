using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public class CashBalanceService(ICashBalanceRepository repository) : ICashBalanceService
{
    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        var cashBalance = await repository.GetByUserIdAsync(userId);
        return cashBalance?.Amount ?? 0m;
    }

    public async Task UpdateManualBalanceAsync(Guid userId, decimal newAmount)
    {
        if (newAmount < 0)
        {
            throw new ArgumentException("Cash balance cannot be negative", nameof(newAmount));
        }

        await UpdateBalanceInternalAsync(userId, newAmount, CashUpdateSource.Manual);
    }

    public async Task ProcessTransactionAsync(Guid userId, TransactionType type, decimal amount)
    {
        var currentBalance = await GetBalanceAsync(userId);
        var newBalance = type switch
        {
            TransactionType.Buy => Math.Max(0, currentBalance - amount),
            TransactionType.Sell => currentBalance + amount,
            TransactionType.Dividend => currentBalance + amount,
            _ => currentBalance
        };

        if (newBalance != currentBalance)
        {
            await UpdateBalanceInternalAsync(userId, newBalance, CashUpdateSource.Transaction);
        }
    }

    public async Task RevertTransactionAsync(Guid userId, TransactionType type, decimal amount)
    {
        var currentBalance = await GetBalanceAsync(userId);
        var newBalance = type switch
        {
            TransactionType.Buy => currentBalance + amount,
            TransactionType.Sell => Math.Max(0, currentBalance - amount),
            TransactionType.Dividend => Math.Max(0, currentBalance - amount),
            _ => currentBalance
        };

        if (newBalance != currentBalance)
        {
            await UpdateBalanceInternalAsync(userId, newBalance, CashUpdateSource.Transaction);
        }
    }

    private async Task UpdateBalanceInternalAsync(Guid userId, decimal amount, CashUpdateSource source)
    {
        var cashBalance = new CashBalance
        {
            UserId = userId,
            Amount = amount,
            LastUpdatedAt = DateTime.UtcNow,
            LastUpdatedSource = source
        };

        await repository.AddOrUpdateAsync(cashBalance);
    }
}
