using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/transactions")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTransaction(CreateTransactionRequest request)
    {
        await transactionService.Create(request);
        return Ok(new { message = "Successfully stored the transaction" });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateTransactionsBulk(List<CreateTransactionRequest> requests)
    {
        var transactions = await transactionService.CreateBulk(requests);
        return Ok(new { message = $"Successfully stored {transactions.Count} transactions" });
    }

    /// <summary>
    /// Gets all transactions for a user, ordered by date descending (newest first).
    /// </summary>
    /// <param name="userId">Optional user ID. If not provided, uses root user.</param>
    /// <returns>List of transactions with security information</returns>
    [HttpGet("{userId?}")]
    public async Task<IActionResult> GetTransactions(Guid? userId)
    {
        var transactions = await transactionService.GetAllByUser(userId);
        return Ok(transactions);
    }

    /// <summary>
    /// Updates an existing transaction for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="transactionId">Transaction ID to update</param>
    /// <param name="request">Transaction update request</param>
    /// <returns>Updated transaction</returns>
    [HttpPut("{userId}/{transactionId}")]
    public async Task<IActionResult> UpdateTransaction(Guid userId, Guid transactionId, UpdateTransactionRequest request)
    {
        var transaction = await transactionService.Update(userId, transactionId, request);
        return Ok(transaction);
    }

    /// <summary>
    /// Deletes a transaction for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="transactionId">Transaction ID to delete</param>
    /// <returns>Success message</returns>
    [HttpDelete("{userId}/{transactionId}")]
    public async Task<IActionResult> DeleteTransaction(Guid userId, Guid transactionId)
    {
        await transactionService.Delete(userId, transactionId);
        return Ok(new { message = "Transaction deleted successfully" });
    }
}
