using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/transactions")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTransactionAsync(CreateTransactionRequest request)
    {
        await transactionService.Create(request);
        return Ok(new { message = "Successfully stored the transaction" });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateTransactionsBulkAsync(List<CreateTransactionRequest> requests)
    {
        var transactions = await transactionService.CreateBulk(requests);
        return Ok(new { message = $"Successfully stored {transactions.Count} transactions" });
    }

    // [HttpGet]
    // public async Task<IActionResult> GetTransactions(Guid userId)
    // {
    //     var transactions = await transactionService
    // }
}
