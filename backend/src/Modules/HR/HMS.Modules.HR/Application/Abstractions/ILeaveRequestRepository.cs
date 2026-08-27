using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Abstractions;

/// <summary>LeaveRequest plus the Employee/LeaveType display fields a list row needs —
/// resolved via a same-schema join in LeaveRequestRepository, mirroring AttendanceWithEmployee.</summary>
internal sealed record LeaveRequestWithDetails(LeaveRequest LeaveRequest, string EmployeeCode, string EmployeeName, string LeaveTypeName);

internal interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken);

    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<LeaveRequestWithDetails> Items, int TotalCount)> GetPagedAsync(LeaveRequestListQuery query, CancellationToken cancellationToken);

    /// <summary>Sum of TotalDays for this employee+leave type's Approved requests whose
    /// StartDate falls in the given calendar year — backs the leave-balance calculation.</summary>
    Task<int> GetApprovedDaysAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken);

    /// <summary>Count of Pending requests across all employees — backs the HR dashboard.</summary>
    Task<int> CountPendingAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
