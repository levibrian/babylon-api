using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ICashBalanceService
{
    Task<decimal> GetBalanceAsync(Guid userId);
    Task UpdateManualBalanceAsync(Guid userId, decimal newAmount);
    Task ProcessTransactionAsync(Guid userId, TransactionType type, decimal amount);
    Task RevertTransactionAsync(Guid userId, TransactionType type, decimal amount);
}
