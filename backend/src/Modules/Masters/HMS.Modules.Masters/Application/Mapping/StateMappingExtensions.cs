using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class StateMappingExtensions
{
    public static StateResponse ToResponse(this State state) => new()
    {
        Id = state.Id,
        Name = state.Name,
    };
}
