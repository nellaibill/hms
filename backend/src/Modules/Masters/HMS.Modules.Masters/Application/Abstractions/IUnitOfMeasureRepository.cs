using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IUnitOfMeasureRepository
{
    Task AddAsync(UnitOfMeasure unitOfMeasure, CancellationToken cancellationToken);

    Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string uomCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<UnitOfMeasure> Items, int TotalCount)> GetPagedAsync(UnitOfMeasureListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
