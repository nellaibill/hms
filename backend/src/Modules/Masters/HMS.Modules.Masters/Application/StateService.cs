using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): StatesController requires a public constructor dependency (CS0051
/// otherwise). Interface and implementation share this file, matching the other Masters
/// entities' {Entity}Service.cs convention. Read-only — States has no admin CRUD in this
/// iteration, only the seeded list (see StateConfiguration).
/// </summary>
public interface IStateService
{
    Task<IReadOnlyList<StateResponse>> GetAllAsync(CancellationToken cancellationToken);
}

internal class StateService : IStateService
{
    private readonly IStateRepository _repository;

    public StateService(IStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StateResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var states = await _repository.GetAllAsync(cancellationToken);
        return states.Select(s => s.ToResponse()).ToList();
    }
}
