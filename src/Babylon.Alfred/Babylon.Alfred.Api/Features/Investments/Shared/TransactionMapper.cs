using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Shared;

/// <summary>
/// Maps transaction entities to DTOs.
/// </summary>
public static class TransactionMapper
{
    /// <summary>
    /// Maps a Transaction entity to TransactionDto.
    /// </summary>
    public static TransactionDto ToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            Ticker = transaction.Security?.Ticker ?? string.Empty,
            SecurityName = transaction.Security?.SecurityName ?? string.Empty,
            SecurityType = transaction.Security?.SecurityType ?? SecurityType.Stock,
            Date = transaction.Date,
            SharesQuantity = transaction.SharesQuantity,
            SharePrice = transaction.SharePrice,
            Fees = transaction.Fees,
            Tax = transaction.Tax,
            TransactionType = transaction.TransactionType
        };
    }

    /// <summary>
    /// Maps a collection of Transaction entities to TransactionDto collection.
    /// </summary>
    public static IEnumerable<TransactionDto> ToDtoCollection(IEnumerable<Transaction> transactions)
    {
        return transactions.Select(ToDto);
    }
}

