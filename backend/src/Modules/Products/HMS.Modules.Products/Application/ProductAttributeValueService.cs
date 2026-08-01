using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

public interface IProductAttributeValueService
{
    Task<Result<ProductAttributeValueResponse>> CreateAsync(Guid productId, CreateProductAttributeValueRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductAttributeValueResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductAttributeValueRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductAttributeValueResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductAttributeValueResponse>> GetPagedAsync(Guid productId, ProductAttributeValueListQuery query, CancellationToken cancellationToken);
}

internal class ProductAttributeValueService : IProductAttributeValueService
{
    private readonly IProductAttributeValueRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IProductAttributeRepository _attributeRepository;

    public ProductAttributeValueService(IProductAttributeValueRepository repository, IProductRepository productRepository, IProductAttributeRepository attributeRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
    }

    public async Task<Result<ProductAttributeValueResponse>> CreateAsync(Guid productId, CreateProductAttributeValueRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(productId, cancellationToken))
        {
            return Result<ProductAttributeValueResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Product '{productId}' was not found.");
        }

        if (!await _attributeRepository.ExistsAsync(request.AttributeId, cancellationToken))
        {
            return Result<ProductAttributeValueResponse>.Failure(ProductsErrorCodes.InvalidReference, $"Attribute '{request.AttributeId}' was not found.");
        }

        if (await _repository.ExistsAsync(productId, request.AttributeId, excludingId: null, cancellationToken))
        {
            return Result<ProductAttributeValueResponse>.Failure(ProductsErrorCodes.DuplicateCode, "A value for this attribute already exists on this product.");
        }

        var value = ProductAttributeValue.Create(productId, request.AttributeId, request.AttributeValue, request.IsActive, actorId);

        await _repository.AddAsync(value, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductAttributeValueResponse>.Success(value.ToResponse());
    }

    public async Task<Result<ProductAttributeValueResponse>> UpdateAsync(Guid productId, Guid id, UpdateProductAttributeValueRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var value = await _repository.GetByIdAsync(id, cancellationToken);
        if (value is null || value.ProductId != productId)
        {
            return Result<ProductAttributeValueResponse>.Failure(ProductsErrorCodes.NotFound, $"Attribute value '{id}' was not found for product '{productId}'.");
        }

        value.Update(request.AttributeValue, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductAttributeValueResponse>.Success(value.ToResponse());
    }

    public async Task<Result<ProductAttributeValueResponse>> GetByIdAsync(Guid productId, Guid id, CancellationToken cancellationToken)
    {
        var value = await _repository.GetByIdAsync(id, cancellationToken);
        return value is null || value.ProductId != productId
            ? Result<ProductAttributeValueResponse>.Failure(ProductsErrorCodes.NotFound, $"Attribute value '{id}' was not found for product '{productId}'.")
            : Result<ProductAttributeValueResponse>.Success(value.ToResponse());
    }

    public async Task<PagedResult<ProductAttributeValueResponse>> GetPagedAsync(Guid productId, ProductAttributeValueListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedByProductAsync(productId, query, cancellationToken);
        return new PagedResult<ProductAttributeValueResponse>(items.Select(v => v.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
