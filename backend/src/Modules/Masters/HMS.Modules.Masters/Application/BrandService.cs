using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IBrandService
{
    Task<Result<BrandResponse>> CreateAsync(CreateBrandRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<BrandResponse>> UpdateAsync(Guid id, UpdateBrandRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<BrandResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<BrandResponse>> GetPagedAsync(BrandListQuery query, CancellationToken cancellationToken);
}

internal class BrandService : IBrandService
{
    private readonly IBrandRepository _repository;

    public BrandService(IBrandRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BrandResponse>> CreateAsync(CreateBrandRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.BrandCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<BrandResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Brand code '{request.BrandCode}' is already in use.");
        }

        var brand = Brand.Create(request.BrandCode, request.BrandName, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(brand, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<BrandResponse>.Success(brand.ToResponse());
    }

    public async Task<Result<BrandResponse>> UpdateAsync(Guid id, UpdateBrandRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
        {
            return Result<BrandResponse>.Failure(MastersErrorCodes.NotFound, $"Brand '{id}' was not found.");
        }

        brand.Update(request.BrandName, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<BrandResponse>.Success(brand.ToResponse());
    }

    public async Task<Result<BrandResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync(id, cancellationToken);
        return brand is null
            ? Result<BrandResponse>.Failure(MastersErrorCodes.NotFound, $"Brand '{id}' was not found.")
            : Result<BrandResponse>.Success(brand.ToResponse());
    }

    public async Task<PagedResult<BrandResponse>> GetPagedAsync(BrandListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<BrandResponse>(items.Select(b => b.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
