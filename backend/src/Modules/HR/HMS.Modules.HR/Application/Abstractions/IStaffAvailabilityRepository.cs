using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as IWeeklyRosterRepository.
/// </summary>
internal interface IStaffAvailabilityRepository
{
    Task AddAsync(StaffAvailability staffAvailability, CancellationToken cancellationToken);

    Task<StaffAvailability?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<StaffAvailability> Items, int TotalCount)> GetPagedAsync(StaffAvailabilityListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
