using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class WardMappingExtensions
{
    public static WardResponse ToResponse(this Ward ward) => new()
    {
        Id = ward.Id,
        Code = ward.Code,
        Name = ward.Name,
        DepartmentId = ward.DepartmentId,
        WardType = ward.WardType,
        IsActive = ward.IsActive,
        CreatedAt = ward.CreatedAt,
        UpdatedAt = ward.UpdatedAt,
    };
}
