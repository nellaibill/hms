using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;

namespace HMS.Modules.Identity.Application.Mapping;

internal static class PermissionMappingExtensions
{
    public static PermissionResponse ToResponse(
        this Permission permission)
    {
        return new PermissionResponse
        {
            Id = permission.Id,
            Module = permission.Module,
            Action = permission.Action,
            Key = permission.Key,
            Label = permission.Label,
            DisplayOrder = permission.DisplayOrder
        };
    }
}
