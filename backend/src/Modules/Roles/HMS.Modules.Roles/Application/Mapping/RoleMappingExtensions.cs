using HMS.Modules.Roles.Contracts;
using HMS.Modules.Roles.Domain;

namespace HMS.Modules.Roles.Application.Mapping;

internal static class RoleMappingExtensions
{
    public static RoleResponse ToResponse(this Role role)
    {
        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            DisplayOrder = role.DisplayOrder,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}