using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

/// <summary>Global attribute-definition catalog — not product-scoped, unlike the other repositories in this module.</summary>
internal interface IProductAttributeRepository
{
    Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken);

    Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string attributeCode, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductAttribute> Items, int TotalCount)> GetPagedAsync(ProductAttributeListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
