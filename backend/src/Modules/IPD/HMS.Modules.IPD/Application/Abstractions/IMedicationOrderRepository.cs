using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IMedicationOrderRepository
{
    Task AddAsync(MedicationOrder order, CancellationToken cancellationToken);

    Task<MedicationOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MedicationOrder>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
