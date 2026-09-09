using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IVitalsReadingRepository
{
    Task AddAsync(VitalsReading reading, CancellationToken cancellationToken);

    Task<IReadOnlyList<VitalsReading>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
