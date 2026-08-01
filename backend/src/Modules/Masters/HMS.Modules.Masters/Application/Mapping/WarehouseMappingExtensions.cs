using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class WarehouseMappingExtensions
{
    public static WarehouseResponse ToResponse(this Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        WarehouseCode = warehouse.WarehouseCode,
        WarehouseName = warehouse.WarehouseName,
        Address = warehouse.Address,
        Country = warehouse.Country,
        State = warehouse.State,
        IsActive = warehouse.IsActive,
        CreatedAt = warehouse.CreatedAt,
        UpdatedAt = warehouse.UpdatedAt,
    };
}
