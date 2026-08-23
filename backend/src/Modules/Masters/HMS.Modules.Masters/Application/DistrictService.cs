using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): StatesController requires a public constructor dependency (CS0051
/// otherwise). Read-only, same as <see cref="IStateService"/> — no admin CRUD in this
/// iteration. A StateId for a state that doesn't exist simply yields an empty list, the same
/// way ConsultantListQuery.DepartmentId behaves for an unknown department — no explicit
/// existence check needed here.
/// </summary>
public interface IDistrictService
{
    Task<IReadOnlyList<DistrictResponse>> GetByStateIdAsync(Guid stateId, CancellationToken cancellationToken);
}

internal class DistrictService : IDistrictService
{
    private readonly IDistrictRepository _repository;

    public DistrictService(IDistrictRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<DistrictResponse>> GetByStateIdAsync(Guid stateId, CancellationToken cancellationToken)
    {
        var districts = await _repository.GetByStateIdAsync(stateId, cancellationToken);
        return districts.Select(d => d.ToResponse()).ToList();
    }
}
