using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IAdmissionRepository
{
    Task AddAsync(Admission admission, CancellationToken cancellationToken);

    Task<Admission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Admission?> GetActiveByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Admission> Items, int TotalCount)> GetPagedAsync(AdmissionListQuery query, CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(AdmissionStatus status, CancellationToken cancellationToken);

    Task<int> CountAdmittedTodayAsync(CancellationToken cancellationToken);

    Task<int> CountDischargedTodayAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
