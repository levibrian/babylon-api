using Babylon.Alfred.Api.Features.Import.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.Import.Services;

public class CsvImportService(
    ITransactionRepository transactionRepository,
    ISecurityRepository securityRepository,
    ITransactionService transactionService)
    : ICsvImportService
{
    public async Task<ImportResult> ImportTradeRepublicCsv(Guid userId, string csvContent)
    {
        var rows = TradeRepublicCsvParser.Parse(csvContent);

        var existingFingerprints = await transactionRepository.GetExistingImportFingerprintsAsync(
            userId, rows.Select(r => r.Fingerprint));

        var newRows = rows.Where(r => !existingFingerprints.Contains(r.Fingerprint)).ToList();
        var skippedDuplicates = rows.Count - newRows.Count;

        var isins = newRows.Select(r => r.Isin).Distinct().ToList();
        var securitiesByIsin = new Dictionary<string, Security>();
        foreach (var isin in isins)
        {
            var security = await securityRepository.GetByIsinAsync(isin);
            if (security != null)
            {
                securitiesByIsin[isin] = security;
            }
        }

        var errors = new List<ImportRowError>();
        var requests = new List<CreateTransactionRequest>();

        foreach (var row in newRows)
        {
            if (!securitiesByIsin.TryGetValue(row.Isin, out var security))
            {
                errors.Add(new ImportRowError(row.Fingerprint, $"No security found for ISIN {row.Isin}"));
                continue;
            }

            requests.Add(new CreateTransactionRequest
            {
                Ticker = security.Ticker,
                TransactionType = row.TransactionType,
                Date = row.Date,
                SharesQuantity = row.SharesQuantity,
                SharePrice = row.SharePrice,
                Fees = row.Fees,
                Tax = row.Tax,
                TotalAmount = row.TotalAmount,
                ImportSource = TradeRepublicCsvParser.Source,
                ImportFingerprint = row.Fingerprint
            });
        }

        await transactionService.CreateBulk(userId, requests);

        return new ImportResult
        {
            Imported = requests.Count,
            SkippedDuplicates = skippedDuplicates,
            Errors = errors
        };
    }
}
