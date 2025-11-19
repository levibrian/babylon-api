using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Models.Responses.Portfolios;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ITransactionService
{
    Task<Transaction> Create(CreateTransactionRequest request);
    Task<IList<Transaction>> CreateBulk(List<CreateTransactionRequest> requests);
    Task<PortfolioTransactionDto> GetById(Guid id);
    Task<IEnumerable<TransactionDto>> GetAllByUser(Guid? userId);
    Task<TransactionDto> Update(Guid userId, Guid transactionId, UpdateTransactionRequest request);
    Task Delete(Guid userId, Guid transactionId);
}
