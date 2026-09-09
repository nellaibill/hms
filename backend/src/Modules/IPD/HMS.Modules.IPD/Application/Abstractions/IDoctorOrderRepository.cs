using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IDoctorOrderRepository
{
    Task AddAsync(DoctorOrder order, CancellationToken cancellationToken);

    Task<DoctorOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorOrder>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
