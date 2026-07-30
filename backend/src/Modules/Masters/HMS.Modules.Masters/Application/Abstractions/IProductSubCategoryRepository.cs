using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IProductSubCategoryRepository
{
    Task AddAsync(ProductSubCategory subCategory, CancellationToken cancellationToken);

    Task<ProductSubCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string subCategoryCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductSubCategory> Items, int TotalCount)> GetPagedAsync(ProductSubCategoryListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
