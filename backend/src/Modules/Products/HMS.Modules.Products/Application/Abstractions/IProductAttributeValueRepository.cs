using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductAttributeValueRepository
{
    Task AddAsync(ProductAttributeValue value, CancellationToken cancellationToken);

    Task<ProductAttributeValue?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid productId, Guid attributeId, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductAttributeValue> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductAttributeValueListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
