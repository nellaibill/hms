using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IProductCategoryService
{
    Task<Result<ProductCategoryResponse>> CreateAsync(CreateProductCategoryRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductCategoryResponse>> UpdateAsync(Guid id, UpdateProductCategoryRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductCategoryResponse>> GetPagedAsync(ProductCategoryListQuery query, CancellationToken cancellationToken);
}

internal class ProductCategoryService : IProductCategoryService
{
    private readonly IProductCategoryRepository _repository;

    public ProductCategoryService(IProductCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductCategoryResponse>> CreateAsync(CreateProductCategoryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.CategoryCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ProductCategoryResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Category code '{request.CategoryCode}' is already in use.");
        }

        if (request.ParentId.HasValue && !await _repository.ExistsAsync(request.ParentId.Value, cancellationToken))
        {
            return Result<ProductCategoryResponse>.Failure(MastersErrorCodes.InvalidReference, $"Parent category '{request.ParentId}' was not found.");
        }

        var category = ProductCategory.Create(request.CategoryCode, request.CategoryName, request.ParentId, request.SortOrder, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(category, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductCategoryResponse>.Success(category.ToResponse());
    }

    public async Task<Result<ProductCategoryResponse>> UpdateAsync(Guid id, UpdateProductCategoryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Result<ProductCategoryResponse>.Failure(MastersErrorCodes.NotFound, $"Product category '{id}' was not found.");
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == id)
            {
                return Result<ProductCategoryResponse>.Failure(MastersErrorCodes.InvalidReference, "A category cannot be its own parent.");
            }

            if (!await _repository.ExistsAsync(request.ParentId.Value, cancellationToken))
            {
                return Result<ProductCategoryResponse>.Failure(MastersErrorCodes.InvalidReference, $"Parent category '{request.ParentId}' was not found.");
            }
        }

        category.Update(request.CategoryName, request.ParentId, request.SortOrder, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductCategoryResponse>.Success(category.ToResponse());
    }

    public async Task<Result<ProductCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        return category is null
            ? Result<ProductCategoryResponse>.Failure(MastersErrorCodes.NotFound, $"Product category '{id}' was not found.")
            : Result<ProductCategoryResponse>.Success(category.ToResponse());
    }

    public async Task<PagedResult<ProductCategoryResponse>> GetPagedAsync(ProductCategoryListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ProductCategoryResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
