using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IManufacturerRepository
{
    Task AddAsync(Manufacturer manufacturer, CancellationToken cancellationToken);

    Task<Manufacturer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string manufacturerCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Manufacturer> Items, int TotalCount)> GetPagedAsync(ManufacturerListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
