using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Domain;

/// <summary>
/// One row from an uploaded import file. RawDataJson/ErrorsJson/MappedRequestJson are opaque,
/// pre-serialized JSON as far as this entity is concerned — the Application layer owns their
/// shape and serializes/deserializes them; keeping that shape out of Domain avoids coupling
/// this entity to the report/review/commit DTOs.
/// </summary>
internal class PatientImportRow : Entity
{
    public Guid BatchId { get; private set; }
    public int RowNumber { get; private set; }
    public string RawDataJson { get; private set; } = null!;
    public ImportRowStatus Status { get; private set; }
    public string? ErrorsJson { get; private set; }

    /// <summary>The fully mapped, already-validated CreatePatientRequest (serialized) for a
    /// Valid row — set once at validate time so the commit pass, which processes each row in
    /// its own DI scope for isolation (see PatientImportCommitBackgroundService), doesn't need
    /// to redo State/District name resolution against Masters for every single row.</summary>
    public string? MappedRequestJson { get; private set; }

    public Guid? CreatedPatientId { get; private set; }

    // Required by EF Core materialization.
    private PatientImportRow()
    {
    }

    private PatientImportRow(Guid id, Guid batchId, int rowNumber, string rawDataJson, ImportRowStatus status, string? errorsJson, string? mappedRequestJson)
        : base(id, null)
    {
        BatchId = batchId;
        RowNumber = rowNumber;
        RawDataJson = rawDataJson;
        Status = status;
        ErrorsJson = errorsJson;
        MappedRequestJson = mappedRequestJson;
    }

    public static PatientImportRow CreateValid(Guid batchId, int rowNumber, string rawDataJson, string mappedRequestJson)
        => new(Guid.CreateVersion7(), batchId, rowNumber, rawDataJson, ImportRowStatus.Valid, null, mappedRequestJson);

    public static PatientImportRow CreateInvalid(Guid batchId, int rowNumber, string rawDataJson, string errorsJson)
        => new(Guid.CreateVersion7(), batchId, rowNumber, rawDataJson, ImportRowStatus.Invalid, errorsJson, null);

    public void MarkCreated(Guid patientId)
    {
        Status = ImportRowStatus.Created;
        CreatedPatientId = patientId;
    }

    public void MarkCommitFailed(string errorsJson)
    {
        Status = ImportRowStatus.CommitFailed;
        ErrorsJson = errorsJson;
    }
}
