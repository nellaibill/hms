using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DesignationMappingExtensions
{
    public static DesignationResponse ToResponse(this Designation designation) => new()
    {
        Id = designation.Id,
        Code = designation.Code,
        Name = designation.Name,
        IsActive = designation.IsActive,
        CreatedAt = designation.CreatedAt,
        UpdatedAt = designation.UpdatedAt,
    };
}
