using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

public interface IProductBarcodeService
{
    Task<Result<ProductBarcodeResponse>> CreateAsync(Guid productId, CreateProductBarcodeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductBarcodeResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductBarcodeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductBarcodeResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductBarcodeResponse>> GetPagedAsync(Guid productId, ProductBarcodeListQuery query, CancellationToken cancellationToken);
}

internal class ProductBarcodeService : IProductBarcodeService
{
    private readonly IProductBarcodeRepository _repository;
    private readonly IProductRepository _productRepository;

    public ProductBarcodeService(IProductBarcodeRepository repository, IProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
    }

    public async Task<Result<ProductBarcodeResponse>> CreateAsync(Guid productId, CreateProductBarcodeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
        {
            return Result<ProductBarcodeResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Product '{productId}' was not found.");
        }

        if (await _repository.ExistsByBarcodeValueAsync(request.BarcodeValue.Trim(), excludingId: null, cancellationToken))
        {
            return Result<ProductBarcodeResponse>.Failure(ProductsErrorCodes.DuplicateCode, $"Barcode value '{request.BarcodeValue}' is already in use.");
        }

        var barcode = ProductBarcode.Create(productId, request.BarcodeType, request.BarcodeValue, request.IsPrimary, request.IsActive, request.Notes, actorId);

        await _repository.AddAsync(barcode, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductBarcodeResponse>.Success(barcode.ToResponse());
    }

    public async Task<Result<ProductBarcodeResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductBarcodeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var barcode = await _repository.GetByIdAsync(id, cancellationToken);
        if (barcode is null || barcode.ProductId != productId)
        {
            return Result<ProductBarcodeResponse>.Failure(ProductsErrorCodes.NotFound, $"Barcode '{id}' was not found for product '{productId}'.");
        }

        barcode.Update(request.BarcodeType, request.IsPrimary, request.IsActive, request.Notes, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductBarcodeResponse>.Success(barcode.ToResponse());
    }

    public async Task<Result<ProductBarcodeResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var barcode = await _repository.GetByIdAsync(id, cancellationToken);
        return barcode is null || barcode.ProductId != productId
            ? Result<ProductBarcodeResponse>.Failure(ProductsErrorCodes.NotFound, $"Barcode '{id}' was not found for product '{productId}'.")
            : Result<ProductBarcodeResponse>.Success(barcode.ToResponse());
    }

    public async Task<PagedResult<ProductBarcodeResponse>> GetPagedAsync(Guid productId, ProductBarcodeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedByProductAsync(productId, query, cancellationToken);
        return new PagedResult<ProductBarcodeResponse>(items.Select(b => b.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
