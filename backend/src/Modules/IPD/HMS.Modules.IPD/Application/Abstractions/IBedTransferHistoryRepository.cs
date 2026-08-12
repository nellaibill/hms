using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// No SaveChangesAsync: history rows are always written inside AdmissionService.TransferBedAsync,
/// which persists them via IAdmissionRepository.SaveChangesAsync on the same DbContext instance.
/// </summary>
internal interface IBedTransferHistoryRepository
{
    Task AddAsync(BedTransferHistory history, CancellationToken cancellationToken);

    Task<IReadOnlyList<BedTransferHistory>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}
