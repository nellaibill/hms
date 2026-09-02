using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — mirrors IPatientRepository.
/// </summary>
internal interface IPatientImportRepository
{
    Task AddBatchAsync(PatientImportBatch batch, CancellationToken cancellationToken);

    Task<PatientImportBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PatientImportBatch> Items, int TotalCount)> GetBatchesPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task AddRowsAsync(IEnumerable<PatientImportRow> rows, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PatientImportRow> Items, int TotalCount)> GetRowsPagedAsync(Guid batchId, ImportRowStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Every row for a batch matching the given status, ordered by RowNumber —
    /// used to build the full error report (unlike GetRowsPagedAsync, not paginated, since the
    /// report needs every matching row).</summary>
    Task<IReadOnlyList<PatientImportRow>> GetAllRowsByStatusAsync(Guid batchId, ImportRowStatus status, CancellationToken cancellationToken);

    /// <summary>Row ids only, for the commit pass to iterate — each row is then re-fetched
    /// individually (GetRowByIdAsync) inside its own DI scope, so this must not return tracked
    /// entities that would leak across scopes.</summary>
    Task<IReadOnlyList<Guid>> GetValidRowIdsAsync(Guid batchId, CancellationToken cancellationToken);

    Task<PatientImportRow?> GetRowByIdAsync(Guid rowId, CancellationToken cancellationToken);

    /// <summary>Detaches every tracked entity — called between chunks of a large validate pass
    /// so the change tracker doesn't hold tens of thousands of rows in memory for the whole
    /// file.</summary>
    void ClearTracking();

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
