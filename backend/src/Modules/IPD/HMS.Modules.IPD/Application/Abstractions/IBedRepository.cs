using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// Consumed directly by AdmissionService too (same-module FK/availability checks go through
/// the repository, not a public service interface — mirrors HR's ShiftAssignmentService
/// using IShiftRepository directly).
/// </summary>
internal interface IBedRepository
{
    Task AddAsync(Bed bed, CancellationToken cancellationToken);

    Task<Bed?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByBedNumberAsync(Guid wardId, string bedNumber, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Bed> Items, int TotalCount)> GetPagedAsync(BedListQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<Bed>> GetAvailableAsync(Guid? wardId, CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(BedStatus status, CancellationToken cancellationToken);

    /// <summary>Bed counts restricted to wards of type ICU — for the dashboard's ICU
    /// occupancy metric.</summary>
    Task<(int Total, int Occupied)> GetIcuOccupancyAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
