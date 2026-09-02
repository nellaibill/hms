using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

public sealed record ImportRowPage(IReadOnlyList<ImportRowResponse> Items, int TotalCount);

/// <summary>
/// Public (not internal): PatientImportController — which ASP.NET Core requires to be public,
/// with a public constructor, for controller discovery and DI activation — takes this as a
/// constructor dependency. A public constructor cannot have an internal parameter type
/// (CS0051), so this interface is the module's deliberate, narrow seam between its public HTTP
/// boundary and its otherwise-internal Application/Domain/Infrastructure layers — mirrors
/// IPatientService.
/// </summary>
public interface IPatientImportService
{
    Task<byte[]> GetTemplateAsync(CancellationToken cancellationToken);

    Task<Result<ImportBatchResponse>> UploadAsync(string fileName, byte[] fileContent, Guid? uploadedBy, CancellationToken cancellationToken);

    Task<Result<ImportBatchResponse>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ImportBatchResponse> Items, int TotalCount)> GetBatchesPagedAsync(ImportBatchListQuery query, CancellationToken cancellationToken);

    Task<Result<ImportRowPage>> GetRowsPagedAsync(Guid batchId, ImportRowListQuery query, CancellationToken cancellationToken);

    /// <summary>An .xlsx of every Invalid/CommitFailed row in the batch, in the template's own
    /// column layout plus a reasons column — empty (header row only) if nothing was skipped.</summary>
    Task<Result<byte[]>> GetReportAsync(Guid batchId, CancellationToken cancellationToken);

    /// <summary>Only valid when the batch is ReadyForReview — writes nothing itself, just flips
    /// the batch to Committing and enqueues the background pass that actually creates patients.</summary>
    Task<Result<ImportBatchResponse>> CommitAsync(Guid batchId, Guid? committedBy, CancellationToken cancellationToken);
}
