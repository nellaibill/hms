using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IDistrictRepository
{
    Task<IReadOnlyList<District>> GetByStateIdAsync(Guid stateId, CancellationToken cancellationToken);
}
