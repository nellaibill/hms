using HMS.Modules.Calendar.Application.Abstractions;
using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Calendar.Infrastructure.Repositories;

internal class EventRepository : IEventRepository
{
    private readonly CalendarDbContext _dbContext;

    public EventRepository(CalendarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Event calendarEvent, CancellationToken cancellationToken)
        => await _dbContext.Events.AddAsync(calendarEvent, cancellationToken);

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExistsHolidayOnDateAsync(DateTime startDate, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Events.AnyAsync(
            e => e.EventType == EventType.Holiday && e.StartDate.Date == startDate.Date && e.Id != excludingId,
            cancellationToken);

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetPagedAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        var events = _dbContext.Events.AsQueryable();

        if (query.EventType.HasValue)
        {
            events = events.Where(e => e.EventType == query.EventType.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            events = events.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            events = events.Where(e => EF.Functions.ILike(e.Title, term) || (e.Description != null && EF.Functions.ILike(e.Description, term)));
        }

        events = ApplySort(events, query.Sort);

        var totalCount = await events.CountAsync(cancellationToken);
        var items = await events.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // Filters to events whose date range intersects the given calendar month — a
    // different read shape over the same table GetPagedAsync already queries, not a
    // new data source. Intersection (not just StartDate falling in the month) so a
    // multi-day event starting in July and ending in August still shows up in both
    // months' views.
    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetForMonthAsync(MonthlyEventQuery query, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(query.Year, query.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        var events = _dbContext.Events
            .Where(e => e.StartDate <= monthEnd && e.EndDate >= monthStart);

        if (query.EventType.HasValue)
        {
            events = events.Where(e => e.EventType == query.EventType.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            events = events.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        events = ApplySort(events, query.Sort);

        var totalCount = await events.CountAsync(cancellationToken);
        var items = await events.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Event> ApplySort(IQueryable<Event> events, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return events.OrderBy(e => e.StartDate);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "title" => descending ? events.OrderByDescending(e => e.Title) : events.OrderBy(e => e.Title),
            "startdate" => descending ? events.OrderByDescending(e => e.StartDate) : events.OrderBy(e => e.StartDate),
            "updatedat" => descending ? events.OrderByDescending(e => e.UpdatedAt) : events.OrderBy(e => e.UpdatedAt),
            _ => descending ? events.OrderByDescending(e => e.StartDate) : events.OrderBy(e => e.StartDate),
        };
    }
}
