using Microsoft.AspNetCore.Mvc;
using Babylon.Alfred.Api.Features.Investments.DTOs;
using Babylon.Alfred.Api.Features.Investments.Services;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("/api/v1/investments/transactions")]
public class TransactionsController(
    IInvestmentsService investmentsService,
    ILogger<TransactionsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transaction = await investmentsService.CreateTransactionAsync(request);
            logger.LogInformation("Created transaction {TransactionId} for asset {AssetSymbol}", 
                transaction.Id, transaction.AssetSymbol);

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating transaction for asset {AssetSymbol}", request.AssetSymbol);
            return Problem("An error occurred while creating the transaction.");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        try
        {
            var transaction = await investmentsService.GetTransactionByIdAsync(id);
            if (transaction == null)
                return NotFound($"Transaction with ID {id} not found.");

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving transaction {TransactionId}", id);
            return Problem("An error occurred while retrieving the transaction.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] string? assetSymbol = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            IEnumerable<TransactionResponse> transactions;

            if (!string.IsNullOrEmpty(assetSymbol))
            {
                transactions = await investmentsService.GetTransactionsByAssetAsync(assetSymbol);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                transactions = await investmentsService.GetTransactionsByDateRangeAsync(startDate.Value, endDate.Value);
            }
            else
            {
                transactions = await investmentsService.GetAllTransactionsAsync();
            }

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving transactions");
            return Problem("An error occurred while retrieving transactions.");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transaction = await investmentsService.UpdateTransactionAsync(id, request);
            logger.LogInformation("Updated transaction {TransactionId} for asset {AssetSymbol}", 
                transaction.Id, transaction.AssetSymbol);

            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Transaction {TransactionId} not found for update", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating transaction {TransactionId}", id);
            return Problem("An error occurred while updating the transaction.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            var deleted = await investmentsService.DeleteTransactionAsync(id);
            if (!deleted)
                return NotFound($"Transaction with ID {id} not found.");

            logger.LogInformation("Deleted transaction {TransactionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting transaction {TransactionId}", id);
            return Problem("An error occurred while deleting the transaction.");
        }
    }
}

