using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as IShiftRepository/IShiftAssignmentRepository.
/// </summary>
internal interface IWeeklyRosterRepository
{
    Task AddAsync(WeeklyRoster weeklyRoster, CancellationToken cancellationToken);

    Task<WeeklyRoster?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Backs the "one roster per department per week" rule in WeeklyRosterService.
    Task<bool> ExistsForDepartmentAndWeekAsync(Guid departmentId, DateOnly weekStartDate, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<WeeklyRoster> Items, int TotalCount)> GetPagedAsync(WeeklyRosterListQuery query, CancellationToken cancellationToken);

    // Read-only view over this same aggregate (rosters whose WeekStartDate falls in the
    // given month) — no new entity, no new table.
    Task<(IReadOnlyList<WeeklyRoster> Items, int TotalCount)> GetForMonthAsync(MonthlyWeeklyRosterQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
