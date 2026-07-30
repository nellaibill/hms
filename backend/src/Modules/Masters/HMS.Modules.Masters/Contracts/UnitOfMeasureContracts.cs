using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateUnitOfMeasureRequest
{
    public string UomCode { get; init; } = string.Empty;
    public string UomName { get; init; } = string.Empty;
    public string? UomType { get; init; }
    public bool IsBaseUnit { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateUnitOfMeasureRequest
{
    public string UomName { get; init; } = string.Empty;
    public string? UomType { get; init; }
    public bool IsBaseUnit { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UnitOfMeasureResponse
{
    public Guid Id { get; init; }
    public string UomCode { get; init; } = string.Empty;
    public string UomName { get; init; } = string.Empty;
    public string? UomType { get; init; }
    public bool IsBaseUnit { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class UnitOfMeasureListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
