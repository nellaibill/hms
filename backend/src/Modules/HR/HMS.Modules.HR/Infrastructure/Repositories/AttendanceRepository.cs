using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class AttendanceRepository : IAttendanceRepository
{
    private readonly HRDbContext _dbContext;

    public AttendanceRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Attendance attendance, CancellationToken cancellationToken)
        => await _dbContext.Attendances.AddAsync(attendance, cancellationToken);

    public Task<Attendance?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Attendances.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken cancellationToken)
        => _dbContext.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == attendanceDate, cancellationToken);

    public Task<bool> ExistsForEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Attendances.AnyAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == attendanceDate && a.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<AttendanceWithEmployee> Items, int TotalCount)> GetPagedAsync(AttendanceListQuery query, CancellationToken cancellationToken)
    {
        var joined =
            from attendance in _dbContext.Attendances
            join employee in _dbContext.Employees on attendance.EmployeeId equals employee.Id
            select new { attendance, employee };

        if (query.EmployeeId.HasValue)
        {
            joined = joined.Where(x => x.attendance.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            joined = joined.Where(x => x.employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.Status.HasValue)
        {
            joined = joined.Where(x => x.attendance.Status == query.Status.Value);
        }

        if (query.DateFrom.HasValue)
        {
            joined = joined.Where(x => x.attendance.AttendanceDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            joined = joined.Where(x => x.attendance.AttendanceDate <= query.DateTo.Value);
        }

        joined = joined.OrderByDescending(x => x.attendance.AttendanceDate).ThenBy(x => x.employee.FirstName);

        var totalCount = await joined.CountAsync(cancellationToken);

        var page = await joined
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = page
            .Select(x => new AttendanceWithEmployee(x.attendance, x.employee.EmployeeCode, $"{x.employee.FirstName} {x.employee.LastName}"))
            .ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<AttendanceStatus, int>> GetStatusCountsForDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var counts = await _dbContext.Attendances
            .Where(a => a.AttendanceDate == date)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
