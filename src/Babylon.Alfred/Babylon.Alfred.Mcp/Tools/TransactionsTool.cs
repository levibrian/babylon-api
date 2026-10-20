using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class TransactionsTool
{
    [McpServerTool(Name = "get_transactions", ReadOnly = true)]
    [Description("Returns all transactions (Buy/Sell/Dividend/Split) for the signed-in Babylon user.")]
    public static Task<IEnumerable<TransactionDto>> GetTransactions(ClaimsPrincipal? user, ITransactionService transactionService)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return transactionService.GetAllByUser(user.GetUserId());
    }
}
