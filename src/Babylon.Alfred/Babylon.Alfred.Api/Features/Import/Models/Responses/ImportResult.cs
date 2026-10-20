namespace Babylon.Alfred.Api.Features.Import.Models.Responses;

public class ImportResult
{
    public int Imported { get; init; }
    public int SkippedDuplicates { get; init; }
    public IList<ImportRowError> Errors { get; init; } = [];
}

public record ImportRowError(string Fingerprint, string Reason);
