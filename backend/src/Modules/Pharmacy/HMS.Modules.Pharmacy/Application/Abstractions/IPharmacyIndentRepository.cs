using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>Ward indents. Saved through IPharmacyUnitOfWork.</summary>
internal interface IPharmacyIndentRepository
{
    Task AddAsync(PharmacyIndent indent, CancellationToken cancellationToken);

    /// <summary>The indent with its lines, tracked.</summary>
    Task<PharmacyIndent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Indents with their lines: open first, urgent before routine, oldest first.</summary>
    Task<(IReadOnlyList<PharmacyIndent> Items, int TotalCount)> GetPagedAsync(IndentListQuery query, CancellationToken cancellationToken);
}
