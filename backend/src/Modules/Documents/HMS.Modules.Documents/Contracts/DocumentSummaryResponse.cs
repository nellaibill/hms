namespace HMS.Modules.Documents.Contracts;

/// <summary>Server-side aggregate backing the KPI cards — computed via a database query
/// (see Infrastructure.Repositories.DocumentRepository.GetSummaryAsync), not by scanning
/// every row in application code the way the original frontend mock did.</summary>
public record DocumentSummaryResponse
{
    public int Total { get; init; }
    public int UploadedToday { get; init; }
    public int Archived { get; init; }
    public long StorageUsedBytes { get; init; }
}
