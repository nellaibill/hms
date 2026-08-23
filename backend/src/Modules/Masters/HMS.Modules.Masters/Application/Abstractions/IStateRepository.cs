using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IStateRepository
{
    Task<IReadOnlyList<State>> GetAllAsync(CancellationToken cancellationToken);
}
