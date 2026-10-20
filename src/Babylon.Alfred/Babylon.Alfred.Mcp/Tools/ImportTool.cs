using System.ComponentModel;
using System.Security.Claims;
using Babylon.Alfred.Api.Features.Import.Models.Responses;
using Babylon.Alfred.Api.Features.Import.Services;
using Babylon.Alfred.Api.Shared.Extensions;
using ModelContextProtocol.Server;

namespace Babylon.Alfred.Mcp.Tools;

[McpServerToolType]
public class ImportTool
{
    [McpServerTool(Name = "import_trade_republic_csv", ReadOnly = false, Idempotent = true)]
    [Description("Imports a Trade Republic \"Transaction export.csv\" for the signed-in Babylon user. Pass the raw CSV file content as csvContent. Re-importing the same file is safe — already-imported rows are skipped by fingerprint.")]
    public static Task<ImportResult> ImportTradeRepublicCsv(ClaimsPrincipal? user, ICsvImportService csvImportService, string csvContent)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Not authenticated");
        }

        return csvImportService.ImportTradeRepublicCsv(user.GetUserId(), csvContent);
    }
}
