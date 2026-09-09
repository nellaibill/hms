using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IMedicationAdministrationRepository
{
    Task AddAsync(MedicationAdministration administration, CancellationToken cancellationToken);

    Task<IReadOnlyList<MedicationAdministration>> GetByMedicationOrderIdAsync(Guid medicationOrderId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
