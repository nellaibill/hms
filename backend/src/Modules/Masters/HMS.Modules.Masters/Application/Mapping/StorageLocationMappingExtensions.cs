using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class StorageLocationMappingExtensions
{
    public static StorageLocationResponse ToResponse(this StorageLocation location) => new()
    {
        Id = location.Id,
        WarehouseId = location.WarehouseId,
        LocationCode = location.LocationCode,
        LocationName = location.LocationName,
        LocationType = location.LocationType,
        ParentLocationId = location.ParentLocationId,
        IsActive = location.IsActive,
        CreatedAt = location.CreatedAt,
        UpdatedAt = location.UpdatedAt,
    };
}
