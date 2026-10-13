using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

public interface IProductBatchService
{
    Task<Result<ProductBatchResponse>> CreateAsync(Guid productId, CreateProductBatchRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <param name="keepPurchaseRate">True when the caller can't see purchase rates
    /// (no pharmacy.view-cost): the stored rate is kept and the request's ignored, so saving
    /// back a form that was shown a withheld rate can't wipe the real one.</param>
    Task<Result<ProductBatchResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductBatchRequest request, Guid? actorId, CancellationToken cancellationToken, bool keepPurchaseRate = false);

    Task<Result<ProductBatchResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk lookup across products, for callers denormalizing batch number/expiry
    /// onto a list (e.g. Pharmacy's stock balances) — ids that don't resolve are simply
    /// absent from the result.</summary>
    Task<IReadOnlyList<ProductBatchResponse>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<PagedResult<ProductBatchResponse>> GetPagedAsync(Guid productId, ProductBatchListQuery query, CancellationToken cancellationToken);
}

internal class ProductBatchService : IProductBatchService
{
    private readonly IProductBatchRepository _repository;
    private readonly IProductRepository _productRepository;

    public ProductBatchService(IProductBatchRepository repository, IProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
    }

    public async Task<Result<ProductBatchResponse>> CreateAsync(Guid productId, CreateProductBatchRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
        {
            return Result<ProductBatchResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Product '{productId}' was not found.");
        }

        if (await _repository.ExistsByBatchNoAsync(productId, request.BatchNo.Trim(), excludingId: null, cancellationToken))
        {
            return Result<ProductBatchResponse>.Failure(ProductsErrorCodes.DuplicateCode, $"Batch number '{request.BatchNo}' is already in use for this product.");
        }

        var batch = ProductBatch.Create(productId, request.BatchNo, request.ManufactureDate, request.ExpiryDate, request.IsActive, actorId);
        batch.SetPrices(request.Mrp, request.PurchaseRate, request.SellingRate, actorId);

        await _repository.AddAsync(batch, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductBatchResponse>.Success(batch.ToResponse());
    }

    public async Task<Result<ProductBatchResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductBatchRequest request, Guid? actorId, CancellationToken cancellationToken, bool keepPurchaseRate = false)
    {
        var batch = await _repository.GetByIdAsync(id, cancellationToken);
        if (batch is null || batch.ProductId != productId)
        {
            return Result<ProductBatchResponse>.Failure(ProductsErrorCodes.NotFound, $"Batch '{id}' was not found for product '{productId}'.");
        }

        batch.Update(request.ManufactureDate, request.ExpiryDate, request.IsActive, actorId);
        batch.SetPrices(request.Mrp, keepPurchaseRate ? batch.PurchaseRate : request.PurchaseRate, request.SellingRate, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductBatchResponse>.Success(batch.ToResponse());
    }

    public async Task<Result<ProductBatchResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetByIdAsync(id, cancellationToken);
        return batch is null || batch.ProductId != productId
            ? Result<ProductBatchResponse>.Failure(ProductsErrorCodes.NotFound, $"Batch '{id}' was not found for product '{productId}'.")
            : Result<ProductBatchResponse>.Success(batch.ToResponse());
    }

    public async Task<IReadOnlyList<ProductBatchResponse>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var batches = await _repository.GetManyByIdsAsync(ids, cancellationToken);
        return batches.Select(b => b.ToResponse()).ToList();
    }

    public async Task<PagedResult<ProductBatchResponse>> GetPagedAsync(Guid productId, ProductBatchListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedByProductAsync(productId, query, cancellationToken);
        return new PagedResult<ProductBatchResponse>(items.Select(b => b.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
