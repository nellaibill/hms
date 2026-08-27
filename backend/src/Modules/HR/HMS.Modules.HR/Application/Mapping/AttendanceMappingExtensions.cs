using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

internal static class AttendanceMappingExtensions
{
    public static AttendanceResponse ToResponse(this Attendance attendance, string employeeCode, string employeeName) => new()
    {
        Id = attendance.Id,
        EmployeeId = attendance.EmployeeId,
        EmployeeCode = employeeCode,
        EmployeeName = employeeName,
        AttendanceDate = attendance.AttendanceDate,
        CheckInTime = attendance.CheckInTime,
        CheckOutTime = attendance.CheckOutTime,
        Status = attendance.Status,
        Remarks = attendance.Remarks,
        CreatedAt = attendance.CreatedAt,
        UpdatedAt = attendance.UpdatedAt,
    };

    public static AttendanceResponse ToResponse(this AttendanceWithEmployee item)
        => item.Attendance.ToResponse(item.EmployeeCode, item.EmployeeName);
}
