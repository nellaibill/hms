using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateStorageLocationRequest
{
    public Guid WarehouseId { get; init; }
    public string LocationCode { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string? LocationType { get; init; }
    public Guid? ParentLocationId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateStorageLocationRequest
{
    public string LocationName { get; init; } = string.Empty;
    public string? LocationType { get; init; }
    public Guid? ParentLocationId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record StorageLocationResponse
{
    public Guid Id { get; init; }
    public Guid WarehouseId { get; init; }
    public string LocationCode { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string? LocationType { get; init; }
    public Guid? ParentLocationId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class StorageLocationListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
    public Guid? WarehouseId { get; set; }
}
