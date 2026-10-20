using Babylon.Alfred.Api.Features.Import.Models.Responses;

namespace Babylon.Alfred.Api.Features.Import.Services;

public interface ICsvImportService
{
    Task<ImportResult> ImportTradeRepublicCsv(Guid userId, string csvContent);
}
