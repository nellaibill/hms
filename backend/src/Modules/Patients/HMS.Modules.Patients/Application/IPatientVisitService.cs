using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Public (not internal): PatientVisitsController — which ASP.NET Core requires to be public,
/// with a public constructor, for controller discovery and DI activation — takes this as a
/// constructor dependency. A public constructor cannot have an internal parameter type
/// (CS0051), so this interface is the module's deliberate, narrow seam between its public
/// HTTP boundary and its otherwise-internal Application/Domain/Infrastructure layers.
/// </summary>
public interface IPatientVisitService
{
    /// <summary>Records one registration/encounter event for an existing patient. All
    /// consultation lines supplied together share the returned VisitId.</summary>
    Task<Result<PatientVisitResponse>> CreateAsync(Guid patientId, CreatePatientVisitRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientVisitResponse>> GetByIdAsync(Guid patientId, Guid visitId, CancellationToken cancellationToken);

    /// <summary>Gives an OP visit its OP No once its consultation fee is paid (ADR-103) — called
    /// by Billing after a consultation line is paid. Safe to call more than once: a visit that
    /// already has a number, or isn't an OP visit, comes back unchanged.</summary>
    Task<Result<PatientVisitResponse>> AssignOpNumberAsync(Guid patientId, Guid visitId, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Gives an IP/Day Care/Observation visit its IP No once the advance on its
    /// admission is paid, or when a supervisor issues it without one (ADR-104) — called by IPD.
    /// Safe to call more than once: a visit that already has a number (an Emergency visit always
    /// does), or is an OP visit, comes back unchanged.</summary>
    Task<Result<PatientVisitResponse>> AssignIpNumberAsync(Guid patientId, Guid visitId, IpNumberTrigger trigger, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Every visit for the patient, newest first.</summary>
    Task<Result<IReadOnlyList<PatientVisitResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Cross-patient, paged, optionally date-range-filtered — backs Patient Reports.</summary>
    Task<PagedResult<PatientVisitResponse>> GetAllAsync(PatientVisitListQuery query, CancellationToken cancellationToken);

    /// <summary>Visits of one type per calendar month (hospital local time) for the last
    /// <paramref name="months"/> months, oldest first, zero-filled — the Executive Dashboard's
    /// OP census (DASH-01).</summary>
    Task<IReadOnlyList<MonthlyTotal>> GetMonthlyCountsAsync(VisitType visitType, int months, CancellationToken cancellationToken);
}
