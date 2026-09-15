using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — Application never references EF Core types.
/// </summary>
internal interface IPatientVisitRepository
{
    Task AddAsync(PatientVisit visit, CancellationToken cancellationToken);

    /// <summary>Loads one visit with its Consultations included.</summary>
    Task<PatientVisit?> GetByIdAsync(Guid visitId, CancellationToken cancellationToken);

    /// <summary>Every visit for a patient, newest first, each with its Consultations included.</summary>
    Task<IReadOnlyList<PatientVisit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Cross-patient, paged, optionally date-range-filtered on CreatedAt — backs the
    /// Patient Reports "visits" breakdown, unlike GetByPatientIdAsync above which is scoped to
    /// one patient.</summary>
    Task<(IReadOnlyList<PatientVisit> Items, int TotalCount)> GetPagedAsync(PatientVisitListQuery query, CancellationToken cancellationToken);

    /// <summary>Loads the owning visit (with its Consultations included) for one consultation
    /// line — used by OpdQueryService.TransitionAsync, which needs the aggregate root to call
    /// a domain transition method on one of its children.</summary>
    Task<PatientVisit?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken);

    /// <summary>The OPD Patient List's backing query — one row per PatientVisitConsultation,
    /// joined with its owning PatientVisit and Patient, paged/filtered per OpdPatientListQuery.
    /// Returns a plain projection (OpdPatientListRow), not domain entities, since callers only
    /// ever read this data. See OpdQueryService.GetPatientListAsync.</summary>
    Task<(IReadOnlyList<OpdPatientListRow> Items, int TotalCount)> GetOpdPatientListPagedAsync(OpdPatientListQuery query, CancellationToken cancellationToken);

    /// <summary>The same joined rows GetOpdPatientListPagedAsync produces, unpaged and without
    /// the Status filter — backs the Consultation List tab's per-consultant summary counts.</summary>
    Task<IReadOnlyList<OpdPatientListRow>> GetOpdConsultationSummaryRowsAsync(OpdConsultationSummaryQuery query, CancellationToken cancellationToken);

    /// <summary>The same joined row shape GetOpdPatientListPagedAsync produces, for exactly one
    /// consultation — backs the OPD Consultation form's read-only header (patient/appointment/
    /// consultant/department), which needs the same denormalized fields the list already
    /// resolves but for a single id rather than a page. Null if the consultation doesn't
    /// exist.</summary>
    Task<OpdPatientListRow?> GetOpdConsultationDetailAsync(Guid consultationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
