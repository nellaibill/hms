using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class UnitConversionMappingExtensions
{
    public static UnitConversionResponse ToResponse(this UnitConversion conversion) => new()
    {
        Id = conversion.Id,
        FromUomId = conversion.FromUomId,
        ToUomId = conversion.ToUomId,
        ConversionFactor = conversion.ConversionFactor,
        IsActive = conversion.IsActive,
        CreatedAt = conversion.CreatedAt,
        UpdatedAt = conversion.UpdatedAt,
    };
}
