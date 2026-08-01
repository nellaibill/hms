using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateWarehouseRequest
{
    public string WarehouseCode { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? State { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateWarehouseRequest
{
    public string WarehouseName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? State { get; init; }
    public bool IsActive { get; init; } = true;
}

public record WarehouseResponse
{
    public Guid Id { get; init; }
    public string WarehouseCode { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? State { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class WarehouseListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
