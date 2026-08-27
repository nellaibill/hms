using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;

namespace HMS.Modules.Documents.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule mirrored from HMS.Modules.Patients — Application never references EF Core
/// types directly.
/// </summary>
internal interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);

    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Document> Items, int TotalCount)> GetPagedAsync(DocumentListQuery query, CancellationToken cancellationToken);

    Task<DocumentSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);

    /// <summary>Count of non-deleted, Available-status documents of the given owner type
    /// whose ExpiryDate falls within [fromDate, toDate] inclusive — backs
    /// IDocumentService.GetExpiringDocumentCountAsync.</summary>
    Task<int> GetExpiringCountAsync(DocumentOwnerType ownerType, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
