using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class BedMappingExtensions
{
    public static BedResponse ToResponse(this Bed bed) => new()
    {
        Id = bed.Id,
        WardId = bed.WardId,
        BedNumber = bed.BedNumber,
        BedType = bed.BedType,
        Status = bed.Status,
        IsActive = bed.IsActive,
        CreatedAt = bed.CreatedAt,
        UpdatedAt = bed.UpdatedAt,
    };
}
