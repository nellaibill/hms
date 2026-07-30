using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateUnitConversionRequest
{
    public Guid FromUomId { get; init; }
    public Guid ToUomId { get; init; }
    public decimal ConversionFactor { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateUnitConversionRequest
{
    public decimal ConversionFactor { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UnitConversionResponse
{
    public Guid Id { get; init; }
    public Guid FromUomId { get; init; }
    public Guid ToUomId { get; init; }
    public decimal ConversionFactor { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class UnitConversionListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
