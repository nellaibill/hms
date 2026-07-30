using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IProductSubCategoryService
{
    Task<Result<ProductSubCategoryResponse>> CreateAsync(CreateProductSubCategoryRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductSubCategoryResponse>> UpdateAsync(Guid id, UpdateProductSubCategoryRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductSubCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductSubCategoryResponse>> GetPagedAsync(ProductSubCategoryListQuery query, CancellationToken cancellationToken);
}

internal class ProductSubCategoryService : IProductSubCategoryService
{
    private readonly IProductSubCategoryRepository _repository;
    private readonly IProductCategoryRepository _categoryRepository;

    public ProductSubCategoryService(IProductSubCategoryRepository repository, IProductCategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<ProductSubCategoryResponse>> CreateAsync(CreateProductSubCategoryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.SubCategoryCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ProductSubCategoryResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Sub category code '{request.SubCategoryCode}' is already in use.");
        }

        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
        {
            return Result<ProductSubCategoryResponse>.Failure(MastersErrorCodes.InvalidReference, $"Category '{request.CategoryId}' was not found.");
        }

        var subCategory = ProductSubCategory.Create(request.SubCategoryCode, request.SubCategoryName, request.CategoryId, request.SortOrder, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(subCategory, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductSubCategoryResponse>.Success(subCategory.ToResponse());
    }

    public async Task<Result<ProductSubCategoryResponse>> UpdateAsync(Guid id, UpdateProductSubCategoryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var subCategory = await _repository.GetByIdAsync(id, cancellationToken);
        if (subCategory is null)
        {
            return Result<ProductSubCategoryResponse>.Failure(MastersErrorCodes.NotFound, $"Product sub category '{id}' was not found.");
        }

        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
        {
            return Result<ProductSubCategoryResponse>.Failure(MastersErrorCodes.InvalidReference, $"Category '{request.CategoryId}' was not found.");
        }

        subCategory.Update(request.SubCategoryName, request.CategoryId, request.SortOrder, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductSubCategoryResponse>.Success(subCategory.ToResponse());
    }

    public async Task<Result<ProductSubCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var subCategory = await _repository.GetByIdAsync(id, cancellationToken);
        return subCategory is null
            ? Result<ProductSubCategoryResponse>.Failure(MastersErrorCodes.NotFound, $"Product sub category '{id}' was not found.")
            : Result<ProductSubCategoryResponse>.Success(subCategory.ToResponse());
    }

    public async Task<PagedResult<ProductSubCategoryResponse>> GetPagedAsync(ProductSubCategoryListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ProductSubCategoryResponse>(items.Select(s => s.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
