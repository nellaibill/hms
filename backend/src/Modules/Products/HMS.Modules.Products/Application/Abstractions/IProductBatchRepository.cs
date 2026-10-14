using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;

namespace HMS.Modules.Products.Application.Abstractions;

internal interface IProductBatchRepository
{
    Task AddAsync(ProductBatch batch, CancellationToken cancellationToken);

    Task<ProductBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk lookup across products — see IProductRepository.GetManyByIdsAsync.</summary>
    Task<IReadOnlyList<ProductBatch>> GetManyByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<ProductBatch?> GetByBatchNoAsync(Guid productId, string batchNo, CancellationToken cancellationToken);

    Task<bool> ExistsByBatchNoAsync(Guid productId, string batchNo, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProductBatch> Items, int TotalCount)> GetPagedByProductAsync(Guid productId, ProductBatchListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
