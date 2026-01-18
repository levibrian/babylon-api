using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class CashBalanceRepository(BabylonDbContext context) : ICashBalanceRepository
{
    public async Task<CashBalance?> GetByUserIdAsync(Guid userId)
    {
        return await context.CashBalances
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<CashBalance> AddOrUpdateAsync(CashBalance cashBalance)
    {
        var existing = await context.CashBalances
            .FirstOrDefaultAsync(c => c.UserId == cashBalance.UserId);

        if (existing == null)
        {
            await context.CashBalances.AddAsync(cashBalance);
        }
        else
        {
            existing.Amount = cashBalance.Amount;
            existing.LastUpdatedAt = cashBalance.LastUpdatedAt;
            existing.LastUpdatedSource = cashBalance.LastUpdatedSource;
            context.CashBalances.Update(existing);
        }

        await context.SaveChangesAsync();
        return cashBalance;
    }
}
