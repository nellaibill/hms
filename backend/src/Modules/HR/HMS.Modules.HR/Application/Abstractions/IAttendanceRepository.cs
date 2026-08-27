using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

/// <summary>Attendance plus the employee display fields a list row needs (EmployeeCode/
/// EmployeeName) — resolved via a same-schema join in AttendanceRepository (see Attendance's
/// own remarks on why this is a real FK/join, unlike Employee's cross-module references).</summary>
internal sealed record AttendanceWithEmployee(Attendance Attendance, string EmployeeCode, string EmployeeName);

internal interface IAttendanceRepository
{
    Task AddAsync(Attendance attendance, CancellationToken cancellationToken);

    Task<Attendance?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken cancellationToken);

    Task<bool> ExistsForEmployeeAndDateAsync(Guid employeeId, DateOnly attendanceDate, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<AttendanceWithEmployee> Items, int TotalCount)> GetPagedAsync(AttendanceListQuery query, CancellationToken cancellationToken);

    /// <summary>Backs the HR dashboard's Present/Absent/OnLeave-today tiles — one grouped
    /// count query for a given calendar date.</summary>
    Task<IReadOnlyDictionary<AttendanceStatus, int>> GetStatusCountsForDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
