using HMS.Modules.Masters.Application;
using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

public interface IProductTaxMappingService
{
    Task<Result<ProductTaxMappingResponse>> CreateAsync(Guid productId, CreateProductTaxMappingRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductTaxMappingResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductTaxMappingRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductTaxMappingResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductTaxMappingResponse>> GetPagedAsync(Guid productId, ProductTaxMappingListQuery query, CancellationToken cancellationToken);
}

internal class ProductTaxMappingService : IProductTaxMappingService
{
    private readonly IProductTaxMappingRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly ITaxService _taxService;

    public ProductTaxMappingService(IProductTaxMappingRepository repository, IProductRepository productRepository, ITaxService taxService)
    {
        _repository = repository;
        _productRepository = productRepository;
        _taxService = taxService;
    }

    public async Task<Result<ProductTaxMappingResponse>> CreateAsync(Guid productId, CreateProductTaxMappingRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
        {
            return Result<ProductTaxMappingResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Product '{productId}' was not found.");
        }

        if (!(await _taxService.GetByIdAsync(request.TaxId, cancellationToken)).IsSuccess)
        {
            return Result<ProductTaxMappingResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Tax '{request.TaxId}' was not found.");
        }

        if (await _repository.ExistsAsync(productId, request.TaxId, request.TaxType.Trim(), excludingId: null, cancellationToken))
        {
            return Result<ProductTaxMappingResponse>.Failure(ProductsErrorCodes.DuplicateCode, "A mapping for this product, tax, and tax type already exists.");
        }

        var mapping = ProductTaxMapping.Create(productId, request.TaxId, request.TaxType, request.IsInclusive, request.IsActive, actorId);

        await _repository.AddAsync(mapping, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductTaxMappingResponse>.Success(mapping.ToResponse());
    }

    public async Task<Result<ProductTaxMappingResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductTaxMappingRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var mapping = await _repository.GetByIdAsync(id, cancellationToken);
        if (mapping is null || mapping.ProductId != productId)
        {
            return Result<ProductTaxMappingResponse>.Failure(ProductsErrorCodes.NotFound, $"Tax mapping '{id}' was not found for product '{productId}'.");
        }

        mapping.Update(request.IsInclusive, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductTaxMappingResponse>.Success(mapping.ToResponse());
    }

    public async Task<Result<ProductTaxMappingResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var mapping = await _repository.GetByIdAsync(id, cancellationToken);
        return mapping is null || mapping.ProductId != productId
            ? Result<ProductTaxMappingResponse>.Failure(ProductsErrorCodes.NotFound, $"Tax mapping '{id}' was not found for product '{productId}'.")
            : Result<ProductTaxMappingResponse>.Success(mapping.ToResponse());
    }

    public async Task<PagedResult<ProductTaxMappingResponse>> GetPagedAsync(Guid productId, ProductTaxMappingListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedByProductAsync(productId, query, cancellationToken);
        return new PagedResult<ProductTaxMappingResponse>(items.Select(t => t.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
