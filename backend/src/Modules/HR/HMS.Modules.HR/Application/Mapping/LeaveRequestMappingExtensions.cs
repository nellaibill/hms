using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

internal static class LeaveRequestMappingExtensions
{
    public static LeaveRequestResponse ToResponse(this LeaveRequest leaveRequest, string employeeCode, string employeeName, string leaveTypeName) => new()
    {
        Id = leaveRequest.Id,
        EmployeeId = leaveRequest.EmployeeId,
        EmployeeCode = employeeCode,
        EmployeeName = employeeName,
        LeaveTypeId = leaveRequest.LeaveTypeId,
        LeaveTypeName = leaveTypeName,
        StartDate = leaveRequest.StartDate,
        EndDate = leaveRequest.EndDate,
        TotalDays = leaveRequest.TotalDays,
        Reason = leaveRequest.Reason,
        Status = leaveRequest.Status,
        ApprovedByUserId = leaveRequest.ApprovedByUserId,
        ApprovedAt = leaveRequest.ApprovedAt,
        DecisionNotes = leaveRequest.DecisionNotes,
        CreatedAt = leaveRequest.CreatedAt,
        UpdatedAt = leaveRequest.UpdatedAt,
    };

    public static LeaveRequestResponse ToResponse(this LeaveRequestWithDetails item)
        => item.LeaveRequest.ToResponse(item.EmployeeCode, item.EmployeeName, item.LeaveTypeName);
}
