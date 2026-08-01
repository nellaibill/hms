using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductBatchRepository
{
    Task AddAsync(ProductBatch batch, CancellationToken cancellationToken);

    Task<ProductBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByBatchNoAsync(Guid productId, string batchNo, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductBatch> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductBatchListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
