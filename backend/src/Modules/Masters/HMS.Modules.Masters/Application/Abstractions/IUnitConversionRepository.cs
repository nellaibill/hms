using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IUnitConversionRepository
{
    Task AddAsync(UnitConversion conversion, CancellationToken cancellationToken);

    Task<UnitConversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid fromUomId, Guid toUomId, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<UnitConversion> Items, int TotalCount)> GetPagedAsync(UnitConversionListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
