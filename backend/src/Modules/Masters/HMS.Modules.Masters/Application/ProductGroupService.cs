using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IProductGroupService
{
    Task<Result<ProductGroupResponse>> CreateAsync(CreateProductGroupRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductGroupResponse>> UpdateAsync(Guid id, UpdateProductGroupRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ProductGroupResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductGroupResponse>> GetPagedAsync(ProductGroupListQuery query, CancellationToken cancellationToken);
}

internal class ProductGroupService : IProductGroupService
{
    private readonly IProductGroupRepository _repository;
    private readonly IProductSubCategoryRepository _subCategoryRepository;

    public ProductGroupService(IProductGroupRepository repository, IProductSubCategoryRepository subCategoryRepository)
    {
        _repository = repository;
        _subCategoryRepository = subCategoryRepository;
    }

    public async Task<Result<ProductGroupResponse>> CreateAsync(CreateProductGroupRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.GroupCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ProductGroupResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Group code '{request.GroupCode}' is already in use.");
        }

        if (await _subCategoryRepository.GetByIdAsync(request.SubCategoryId, cancellationToken) is null)
        {
            return Result<ProductGroupResponse>.Failure(MastersErrorCodes.InvalidReference, $"Sub category '{request.SubCategoryId}' was not found.");
        }

        var group = ProductGroup.Create(request.GroupCode, request.GroupName, request.SubCategoryId, request.SortOrder, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(group, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductGroupResponse>.Success(group.ToResponse());
    }

    public async Task<Result<ProductGroupResponse>> UpdateAsync(Guid id, UpdateProductGroupRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdAsync(id, cancellationToken);
        if (group is null)
        {
            return Result<ProductGroupResponse>.Failure(MastersErrorCodes.NotFound, $"Product group '{id}' was not found.");
        }

        if (await _subCategoryRepository.GetByIdAsync(request.SubCategoryId, cancellationToken) is null)
        {
            return Result<ProductGroupResponse>.Failure(MastersErrorCodes.InvalidReference, $"Sub category '{request.SubCategoryId}' was not found.");
        }

        group.Update(request.GroupName, request.SubCategoryId, request.SortOrder, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ProductGroupResponse>.Success(group.ToResponse());
    }

    public async Task<Result<ProductGroupResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdAsync(id, cancellationToken);
        return group is null
            ? Result<ProductGroupResponse>.Failure(MastersErrorCodes.NotFound, $"Product group '{id}' was not found.")
            : Result<ProductGroupResponse>.Success(group.ToResponse());
    }

    public async Task<PagedResult<ProductGroupResponse>> GetPagedAsync(ProductGroupListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ProductGroupResponse>(items.Select(g => g.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
