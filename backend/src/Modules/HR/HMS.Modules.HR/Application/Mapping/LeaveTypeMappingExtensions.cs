using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

internal static class LeaveTypeMappingExtensions
{
    public static LeaveTypeResponse ToResponse(this LeaveType leaveType) => new()
    {
        Id = leaveType.Id,
        Code = leaveType.Code,
        Name = leaveType.Name,
        MaxDaysPerYear = leaveType.MaxDaysPerYear,
        IsPaid = leaveType.IsPaid,
        IsActive = leaveType.IsActive,
        CreatedAt = leaveType.CreatedAt,
        UpdatedAt = leaveType.UpdatedAt,
    };
}
