using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IStorageLocationService
{
    Task<Result<StorageLocationResponse>> CreateAsync(CreateStorageLocationRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StorageLocationResponse>> UpdateAsync(Guid id, UpdateStorageLocationRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StorageLocationResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<StorageLocationResponse>> GetPagedAsync(StorageLocationListQuery query, CancellationToken cancellationToken);
}

internal class StorageLocationService : IStorageLocationService
{
    private readonly IStorageLocationRepository _repository;
    private readonly IWarehouseRepository _warehouseRepository;

    public StorageLocationService(IStorageLocationRepository repository, IWarehouseRepository warehouseRepository)
    {
        _repository = repository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<StorageLocationResponse>> CreateAsync(CreateStorageLocationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            return Result<StorageLocationResponse>.Failure(MastersErrorCodes.InvalidReference, $"Warehouse '{request.WarehouseId}' was not found.");
        }

        var code = request.LocationCode.Trim().ToUpperInvariant();
        if (await _repository.ExistsByCodeAsync(request.WarehouseId, code, excludingId: null, cancellationToken))
        {
            return Result<StorageLocationResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Location code '{request.LocationCode}' is already in use within this warehouse.");
        }

        if (request.ParentLocationId.HasValue && !await _repository.ExistsInWarehouseAsync(request.ParentLocationId.Value, request.WarehouseId, cancellationToken))
        {
            return Result<StorageLocationResponse>.Failure(MastersErrorCodes.InvalidReference, "Parent location must exist and belong to the same warehouse.");
        }

        var location = StorageLocation.Create(request.WarehouseId, request.LocationCode, request.LocationName, request.LocationType, request.ParentLocationId, request.IsActive, actorId);

        await _repository.AddAsync(location, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<StorageLocationResponse>.Success(location.ToResponse());
    }

    public async Task<Result<StorageLocationResponse>> UpdateAsync(Guid id, UpdateStorageLocationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var location = await _repository.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return Result<StorageLocationResponse>.Failure(MastersErrorCodes.NotFound, $"Storage location '{id}' was not found.");
        }

        if (request.ParentLocationId.HasValue)
        {
            if (request.ParentLocationId.Value == id)
            {
                return Result<StorageLocationResponse>.Failure(MastersErrorCodes.InvalidReference, "A storage location cannot be its own parent.");
            }

            if (!await _repository.ExistsInWarehouseAsync(request.ParentLocationId.Value, location.WarehouseId, cancellationToken))
            {
                return Result<StorageLocationResponse>.Failure(MastersErrorCodes.InvalidReference, "Parent location must exist and belong to the same warehouse.");
            }
        }

        location.Update(request.LocationName, request.LocationType, request.ParentLocationId, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<StorageLocationResponse>.Success(location.ToResponse());
    }

    public async Task<Result<StorageLocationResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await _repository.GetByIdAsync(id, cancellationToken);
        return location is null
            ? Result<StorageLocationResponse>.Failure(MastersErrorCodes.NotFound, $"Storage location '{id}' was not found.")
            : Result<StorageLocationResponse>.Success(location.ToResponse());
    }

    public async Task<PagedResult<StorageLocationResponse>> GetPagedAsync(StorageLocationListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<StorageLocationResponse>(items.Select(s => s.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
