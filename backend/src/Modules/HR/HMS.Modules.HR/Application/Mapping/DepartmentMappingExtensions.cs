using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

internal static class DepartmentMappingExtensions
{
    public static DepartmentResponse ToResponse(this Department department) => new()
    {
        Id = department.Id,
        Code = department.Code,
        Name = department.Name,
        IsActive = department.IsActive,
        CreatedAt = department.CreatedAt,
        UpdatedAt = department.UpdatedAt,
    };
}
