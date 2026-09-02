using System.Text.Json;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Mapping;

internal static class PatientImportMappingExtensions
{
    public static ImportBatchResponse ToResponse(this PatientImportBatch batch) => new()
    {
        Id = batch.Id,
        FileName = batch.FileName,
        Status = batch.Status,
        TotalRows = batch.TotalRows,
        ValidRows = batch.ValidRows,
        InvalidRows = batch.InvalidRows,
        CreatedRows = batch.CreatedRows,
        CommitFailedRows = batch.CommitFailedRows,
        UploadedAt = batch.CreatedAt,
        UploadedBy = batch.CreatedBy,
        CommittedAt = batch.CommittedAt,
        CommittedBy = batch.CommittedBy,
    };

    public static ImportRowResponse ToResponse(this PatientImportRow row) => new()
    {
        Id = row.Id,
        RowNumber = row.RowNumber,
        Status = row.Status,
        RawData = row.DeserializeRawData(),
        Errors = row.DeserializeErrors(),
        CreatedPatientId = row.CreatedPatientId,
    };

    public static IReadOnlyDictionary<string, string?> DeserializeRawData(this PatientImportRow row)
        => JsonSerializer.Deserialize<Dictionary<string, string?>>(row.RawDataJson) ?? new Dictionary<string, string?>();

    public static IReadOnlyList<ImportRowError> DeserializeErrors(this PatientImportRow row)
        => string.IsNullOrEmpty(row.ErrorsJson)
            ? []
            : JsonSerializer.Deserialize<List<ImportRowError>>(row.ErrorsJson) ?? [];

    public static CreatePatientRequest? DeserializeMappedRequest(this PatientImportRow row)
        => string.IsNullOrEmpty(row.MappedRequestJson)
            ? null
            : JsonSerializer.Deserialize<CreatePatientRequest>(row.MappedRequestJson);
}
