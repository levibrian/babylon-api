using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        this.transactionService = transactionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTransactionsAsync()
    {
        var transactions = await transactionService.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransactionAsync(CreateTransactionRequest request)
    {
        await transactionService.CreateAsync(request);
        return Ok(new { message = "Successfully stored the transaction" });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateTransactionsBulkAsync(List<CreateTransactionRequest> requests)
    {
        var transactions = await transactionService.CreateBulkAsync(requests);
        return Ok(new { message = $"Successfully stored {transactions.Count} transactions", transactions });
    }
}
