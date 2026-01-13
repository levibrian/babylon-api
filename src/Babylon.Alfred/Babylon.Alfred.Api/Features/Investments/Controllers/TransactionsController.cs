using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/transactions")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTransaction(CreateTransactionRequest request)
    {
        var userId = User.GetUserId();
        await transactionService.Create(userId, request);
        return Ok(new { message = "Successfully stored the transaction" });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateTransactionsBulk(List<CreateTransactionRequest> requests)
    {
        var userId = User.GetUserId();
        var transactions = await transactionService.CreateBulk(userId, requests);
        return Ok(new { message = $"Successfully stored {transactions.Count} transactions" });
    }

    /// <summary>
    /// Gets all transactions for the authenticated user, ordered by date descending (newest first).
    /// </summary>
    /// <returns>List of transactions with security information</returns>
    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        var userId = User.GetUserId();
        var transactions = await transactionService.GetAllByUser(userId);
        return Ok(transactions);
    }

    /// <summary>
    /// Updates an existing transaction for the authenticated user.
    /// </summary>
    /// <param name="transactionId">Transaction ID to update</param>
    /// <param name="request">Transaction update request</param>
    /// <returns>Updated transaction</returns>
    [HttpPut("{transactionId}")]
    public async Task<IActionResult> UpdateTransaction(Guid transactionId, UpdateTransactionRequest request)
    {
        var userId = User.GetUserId();
        var transaction = await transactionService.Update(userId, transactionId, request);
        return Ok(transaction);
    }

    /// <summary>
    /// Deletes a transaction for the authenticated user.
    /// </summary>
    /// <param name="transactionId">Transaction ID to delete</param>
    /// <returns>Success message</returns>
    [HttpDelete("{transactionId}")]
    public async Task<IActionResult> DeleteTransaction(Guid transactionId)
    {
        var userId = User.GetUserId();
        await transactionService.Delete(userId, transactionId);
        return Ok(new { message = "Transaction deleted successfully" });
    }
}
