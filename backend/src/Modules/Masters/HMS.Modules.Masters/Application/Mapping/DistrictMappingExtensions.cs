using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DistrictMappingExtensions
{
    public static DistrictResponse ToResponse(this District district) => new()
    {
        Id = district.Id,
        Name = district.Name,
        StateId = district.StateId,
    };
}
