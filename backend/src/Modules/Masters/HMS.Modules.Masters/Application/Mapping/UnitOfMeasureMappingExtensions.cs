using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class UnitOfMeasureMappingExtensions
{
    public static UnitOfMeasureResponse ToResponse(this UnitOfMeasure unitOfMeasure) => new()
    {
        Id = unitOfMeasure.Id,
        UomCode = unitOfMeasure.UomCode,
        UomName = unitOfMeasure.UomName,
        UomType = unitOfMeasure.UomType,
        IsBaseUnit = unitOfMeasure.IsBaseUnit,
        IsActive = unitOfMeasure.IsActive,
        CreatedAt = unitOfMeasure.CreatedAt,
        UpdatedAt = unitOfMeasure.UpdatedAt,
    };
}
