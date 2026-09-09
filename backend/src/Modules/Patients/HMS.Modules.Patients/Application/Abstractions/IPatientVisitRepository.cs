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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
