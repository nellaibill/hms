using HMS.Modules.DischargeSummary.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.DischargeSummary.Application;

/// <summary>
/// Public (not internal): DischargeSummariesController — which ASP.NET Core requires to be
/// public, with a public constructor, for controller discovery and DI activation — takes
/// this as a constructor dependency. A public constructor cannot have an internal parameter
/// type (CS0051), so this interface is the module's deliberate, narrow seam between its
/// public HTTP boundary and its otherwise-internal Application/Domain/Infrastructure layers.
/// Mirrors HMS.Modules.IPD.Application.IAdmissionService.
/// </summary>
public interface IDischargeSummaryService
{
    /// <summary>Creates a Draft for an already-Discharged admission. Fails with
    /// DischargeSummaryErrorCodes.InvalidAdmission if the admission doesn't exist,
    /// AdmissionNotDischarged if its Status isn't Discharged yet, or AlreadyExists if one
    /// already exists for this AdmissionId.</summary>
    Task<Result<DischargeSummaryResponse>> CreateDraftAsync(Guid admissionId, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DischargeSummaryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<DischargeSummaryResponse>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    /// <summary>Full-record update of every clinical/examination/vitals/course/surgical/
    /// advice field. Fails with DischargeSummaryErrorCodes.NotDraft once Finalized.</summary>
    Task<Result<DischargeSummaryResponse>> UpdateAsync(Guid id, UpdateDischargeSummaryRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Transitions Draft → Finalized, stamping FinalizedAt/FinalizedByUserId. Fails
    /// with DischargeSummaryErrorCodes.AlreadyFinalized if already Finalized.</summary>
    Task<Result<DischargeSummaryResponse>> FinalizeAsync(Guid id, FinalizeDischargeSummaryRequest request, Guid? actorId, CancellationToken cancellationToken);
}
