using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Domain;

namespace HMS.Modules.Calendar.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as every other module's repository interface.
/// </summary>
internal interface IEventRepository
{
    Task AddAsync(Event calendarEvent, CancellationToken cancellationToken);

    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Backs the "Holiday dates must be unique" rule in EventService — true if another,
    // non-deleted Holiday event already starts on the same calendar date.
    Task<bool> ExistsHolidayOnDateAsync(DateTime startDate, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Event> Items, int TotalCount)> GetPagedAsync(EventListQuery query, CancellationToken cancellationToken);

    // Read-only view over the same table (events whose range intersects the given
    // month) — not a new data source, mirrors IWeeklyRosterRepository.GetForMonthAsync.
    Task<(IReadOnlyList<Event> Items, int TotalCount)> GetForMonthAsync(MonthlyEventQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
