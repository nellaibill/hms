using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

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
