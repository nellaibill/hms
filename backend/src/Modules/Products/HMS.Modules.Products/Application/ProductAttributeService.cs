using HMS.Modules.Products.Application.Abstractions;
using HMS.Modules.Products.Application.Mapping;
using HMS.Modules.Products.Contracts;
using HMS.Modules.Products.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Products.Application;

/// <summary>Global attribute-definition catalog — not product-scoped (contrast with the other services in this module, which take a productId).</summary>
public interface IProductAttributeService
{
    Task<Result<ProductAttributeResponse>> CreateAsync(CreateProductAttributeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductAttributeResponse>> UpdateAsync(Guid id, UpdateProductAttributeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductAttributeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductAttributeResponse>> GetPagedAsync(ProductAttributeListQuery query, CancellationToken cancellationToken);
}

internal class ProductAttributeService : IProductAttributeService
{
    private readonly IProductAttributeRepository _repository;

    public ProductAttributeService(IProductAttributeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductAttributeResponse>> CreateAsync(CreateProductAttributeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.AttributeCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ProductAttributeResponse>.Failure(ProductsErrorCodes.DuplicateCode, $"Attribute code '{request.AttributeCode}' is already in use.");
        }

        var attribute = ProductAttribute.Create(request.AttributeCode, request.AttributeName, request.DataType, request.IsMandatory, request.IsActive, actorId);

        await _repository.AddAsync(attribute, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductAttributeResponse>.Success(attribute.ToResponse());
    }

    public async Task<Result<ProductAttributeResponse>> UpdateAsync(Guid id, UpdateProductAttributeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var attribute = await _repository.GetByIdAsync(id, cancellationToken);
        if (attribute is null)
        {
            return Result<ProductAttributeResponse>.Failure(ProductsErrorCodes.NotFound, $"Attribute '{id}' was not found.");
        }

        attribute.Update(request.AttributeName, request.DataType, request.IsMandatory, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductAttributeResponse>.Success(attribute.ToResponse());
    }

    public async Task<Result<ProductAttributeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var attribute = await _repository.GetByIdAsync(id, cancellationToken);
        return attribute is null
            ? Result<ProductAttributeResponse>.Failure(ProductsErrorCodes.NotFound, $"Attribute '{id}' was not found.")
            : Result<ProductAttributeResponse>.Success(attribute.ToResponse());
    }

    public async Task<PagedResult<ProductAttributeResponse>> GetPagedAsync(ProductAttributeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ProductAttributeResponse>(items.Select(a => a.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
