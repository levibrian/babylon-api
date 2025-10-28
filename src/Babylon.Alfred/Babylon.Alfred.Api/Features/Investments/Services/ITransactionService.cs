using Babylon.Alfred.Api.Features.Investments.Models;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Investments.Services;

public interface ITransactionService
{
    Task<Transaction> Create(CreateTransactionRequest request);
    Task<IList<Transaction>> CreateBulk(List<CreateTransactionRequest> requests);
}
