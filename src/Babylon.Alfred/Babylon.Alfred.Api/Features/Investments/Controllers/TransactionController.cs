using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Features.Investments.Services;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[ApiController]
[Route("api/v1/transactions")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public IActionResult GetAllTransactions()
    {
        var transactions = _transactionService.GetAllTransactions();
        return Ok(transactions);
    }

    [HttpPost]
    public IActionResult CreateTransaction(CreateTransactionRequest transaction)
    {
        _transactionService.CreateTransaction(transaction);
        return Ok();
    }
}
