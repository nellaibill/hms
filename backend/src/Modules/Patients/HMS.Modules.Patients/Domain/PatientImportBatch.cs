using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One uploaded Excel file's import run. Tracks aggregate row counts rather than a navigation
/// collection of its <see cref="PatientImportRow"/>s — a batch can have tens of thousands of
/// rows, and every operation on the batch itself (status, counters) only needs the totals, not
/// every row loaded into memory. Rows are queried directly by BatchId for the review UI — see
/// PatientImportRowConfiguration.
/// </summary>
internal class PatientImportBatch : Entity
{
    public string FileName { get; private set; } = null!;
    public ImportBatchStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int InvalidRows { get; private set; }
    public int CreatedRows { get; private set; }
    public int CommitFailedRows { get; private set; }
    public Guid? CommittedBy { get; private set; }
    public DateTime? CommittedAt { get; private set; }

    // Required by EF Core materialization.
    private PatientImportBatch()
    {
    }

    private PatientImportBatch(Guid id, string fileName, Guid? uploadedBy)
        : base(id, uploadedBy)
    {
        FileName = fileName;
        Status = ImportBatchStatus.Validating;
    }

    public static PatientImportBatch Create(string fileName, Guid? uploadedBy)
    {
        Guard.AgainstNullOrWhiteSpace(fileName, nameof(fileName));
        return new PatientImportBatch(Guid.CreateVersion7(), fileName.Trim(), uploadedBy);
    }

    /// <summary>Called once the validate pass has parsed and checked every row. No
    /// patients/addresses exist yet at this point — that only happens after StartCommit.</summary>
    public void MarkReadyForReview(int totalRows, int validRows, int invalidRows)
    {
        TotalRows = totalRows;
        ValidRows = validRows;
        InvalidRows = invalidRows;
        Status = ImportBatchStatus.ReadyForReview;
    }

    /// <summary>The file itself couldn't be processed (bad format, corrupt, empty) — distinct
    /// from individual rows being Invalid, which is an expected ReadyForReview outcome.</summary>
    public void MarkValidationFailed() => Status = ImportBatchStatus.Failed;

    public void StartCommit(Guid? committedBy)
    {
        if (Status != ImportBatchStatus.ReadyForReview)
        {
            throw new InvalidOperationException($"Cannot commit an import batch in status '{Status}'.");
        }

        Status = ImportBatchStatus.Committing;
        CommittedBy = committedBy;
    }

    public void CompleteCommit(int createdRows, int commitFailedRows)
    {
        CreatedRows = createdRows;
        CommitFailedRows = commitFailedRows;
        CommittedAt = DateTime.UtcNow;
        Status = ImportBatchStatus.Completed;
    }
}
