using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class WeeklyRosterRepository : IWeeklyRosterRepository
{
    private readonly HRDbContext _dbContext;

    public WeeklyRosterRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WeeklyRoster weeklyRoster, CancellationToken cancellationToken)
        => await _dbContext.WeeklyRosters.AddAsync(weeklyRoster, cancellationToken);

    public Task<WeeklyRoster?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.WeeklyRosters.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> ExistsForDepartmentAndWeekAsync(Guid departmentId, DateOnly weekStartDate, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.WeeklyRosters.AnyAsync(
            w => w.DepartmentId == departmentId && w.WeekStartDate == weekStartDate && w.Id != excludingId,
            cancellationToken);

    // No Search filtering — WeeklyRoster has no free-text field (no Code/Name/Remarks) to
    // match against, unlike Shift (Code/Name) or ShiftAssignment (Remarks). Search is
    // accepted (inherited from PagedRequest) but simply has nothing to filter on here.
    public async Task<(IReadOnlyList<WeeklyRoster> Items, int TotalCount)> GetPagedAsync(WeeklyRosterListQuery query, CancellationToken cancellationToken)
    {
        var rosters = _dbContext.WeeklyRosters.AsQueryable();

        rosters = ApplySort(rosters, query.Sort);

        var totalCount = await rosters.CountAsync(cancellationToken);
        var items = await rosters.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // Filters to rosters whose WeekStartDate falls within the given calendar month — a
    // different read shape over the same table GetPagedAsync already queries, not a new
    // data source.
    public async Task<(IReadOnlyList<WeeklyRoster> Items, int TotalCount)> GetForMonthAsync(MonthlyWeeklyRosterQuery query, CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(query.Year, query.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var rosters = _dbContext.WeeklyRosters
            .Where(w => w.WeekStartDate >= monthStart && w.WeekStartDate <= monthEnd);

        rosters = ApplySort(rosters, query.Sort);

        var totalCount = await rosters.CountAsync(cancellationToken);
        var items = await rosters.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<WeeklyRoster> ApplySort(IQueryable<WeeklyRoster> rosters, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return rosters.OrderByDescending(w => w.WeekStartDate);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "weekstartdate" => descending ? rosters.OrderByDescending(w => w.WeekStartDate) : rosters.OrderBy(w => w.WeekStartDate),
            "updatedat" => descending ? rosters.OrderByDescending(w => w.UpdatedAt) : rosters.OrderBy(w => w.UpdatedAt),
            _ => descending ? rosters.OrderByDescending(w => w.WeekStartDate) : rosters.OrderBy(w => w.WeekStartDate),
        };
    }
}
