using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Public (not internal): OpdController — which ASP.NET Core requires to be public, with a
/// public constructor, for controller discovery and DI activation — takes this as a
/// constructor dependency. A public constructor cannot have an internal parameter type
/// (CS0051), so this interface is the module's deliberate, narrow seam between its public
/// HTTP boundary and its otherwise-internal Application/Domain/Infrastructure layers, same
/// reasoning as IPatientVisitService.
/// </summary>
public interface IOpdQueryService
{
    /// <summary>The OPD Patient List — one row per PatientVisitConsultation matching the
    /// query's filters, paged, ordered by AppointmentTime ascending (earliest appointment
    /// first, matching how a front desk actually works a queue).</summary>
    Task<PagedResult<OpdPatientListItem>> GetPatientListAsync(OpdPatientListQuery query, CancellationToken cancellationToken);

    /// <summary>The OPD Consultation List — one row per consultant, counting how many of
    /// their matching consultations are Waiting/InConsultation/Completed.</summary>
    Task<Result<IReadOnlyList<OpdConsultationSummaryItem>>> GetConsultationSummaryAsync(OpdConsultationSummaryQuery query, CancellationToken cancellationToken);

    /// <summary>The same denormalized shape GetPatientListAsync's rows have, for exactly one
    /// consultation — backs the OPD Consultation form's read-only header. Used by
    /// HMS.Modules.OpdConsultation (a different module) as its one seam into Patients' OPD read
    /// side, so that module never needs its own Patient/Department/Consultant lookups.</summary>
    Task<Result<OpdPatientListItem>> GetConsultationDetailAsync(Guid consultationId, CancellationToken cancellationToken);

    /// <summary>Advances one consultation's queue status. transitionAction is one of
    /// "check-in", "start-consultation", "complete", "cancel", "no-show" — matching
    /// OpdController's route segments exactly.</summary>
    Task<Result<VisitConsultationResponse>> TransitionAsync(Guid consultationId, string transitionAction, Guid? actorId, CancellationToken cancellationToken);
}
