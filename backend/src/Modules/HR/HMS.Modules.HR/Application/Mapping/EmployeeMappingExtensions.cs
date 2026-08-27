using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

internal static class EmployeeMappingExtensions
{
    /// <summary>Raw mapping — DepartmentName/DesignationName/ReportingManagerName are left
    /// null. Used for paged list results; see EmployeeContracts.EmployeeResponse's remarks for
    /// why enrichment is reserved for the single-record GetByIdAsync call.</summary>
    public static EmployeeResponse ToResponse(this Employee employee) => new()
    {
        Id = employee.Id,
        EmployeeCode = employee.EmployeeCode,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Gender = employee.Gender,
        DateOfBirth = employee.DateOfBirth,
        Phone = employee.Phone,
        Email = employee.Email,
        Address = employee.Address,
        EmergencyContactName = employee.EmergencyContactName,
        EmergencyContactPhone = employee.EmergencyContactPhone,
        DepartmentId = employee.DepartmentId,
        DesignationId = employee.DesignationId,
        EmployeeType = employee.EmployeeType,
        JoiningDate = employee.JoiningDate,
        EmploymentStatus = employee.EmploymentStatus,
        ReportingManagerId = employee.ReportingManagerId,
        ProfilePhotoUrl = employee.ProfilePhotoUrl,
        UserId = employee.UserId,
        IsActive = employee.IsActive,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt,
    };
}
