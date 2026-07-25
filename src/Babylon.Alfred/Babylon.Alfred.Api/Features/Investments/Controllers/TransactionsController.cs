using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Controllers;
using Babylon.Alfred.Api.Shared.Extensions;
using Babylon.Alfred.Api.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Babylon.Alfred.Api.Features.Investments.Controllers;

[Authorize]
[Route("api/v1/transactions")]
public class TransactionsController(ITransactionService transactionService) : BabylonControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateTransaction(CreateTransactionRequest request)
    {
        await transactionService.Create(User.GetUserId(), request);
        return Success();
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<object>>> CreateTransactionsBulk(List<CreateTransactionRequest> requests)
    {
        await transactionService.CreateBulk(User.GetUserId(), requests);
        return Success();
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TransactionDto>>>> GetTransactions()
    {
        var transactions = await transactionService.GetAllByUser(User.GetUserId());
        return Success(transactions);
    }

    [HttpPut("{transactionId}")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> UpdateTransaction(
        Guid transactionId,
        UpdateTransactionRequest request)
    {
        var transaction = await transactionService.Update(User.GetUserId(), transactionId, request);
        return Success(transaction);
    }

    [HttpDelete("{transactionId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTransaction(Guid transactionId)
    {
        await transactionService.Delete(User.GetUserId(), transactionId);
        return Success();
    }
}
