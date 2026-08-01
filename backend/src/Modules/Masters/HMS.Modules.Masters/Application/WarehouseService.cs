using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IWarehouseService
{
    Task<Result<WarehouseResponse>> CreateAsync(CreateWarehouseRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WarehouseResponse>> UpdateAsync(Guid id, UpdateWarehouseRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WarehouseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<WarehouseResponse>> GetPagedAsync(WarehouseListQuery query, CancellationToken cancellationToken);
}

internal class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repository;

    public WarehouseService(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WarehouseResponse>> CreateAsync(CreateWarehouseRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.WarehouseCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<WarehouseResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Warehouse code '{request.WarehouseCode}' is already in use.");
        }

        var warehouse = Warehouse.Create(request.WarehouseCode, request.WarehouseName, request.Address, request.Country, request.State, request.IsActive, actorId);

        await _repository.AddAsync(warehouse, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WarehouseResponse>.Success(warehouse.ToResponse());
    }

    public async Task<Result<WarehouseResponse>> UpdateAsync(Guid id, UpdateWarehouseRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var warehouse = await _repository.GetByIdAsync(id, cancellationToken);
        if (warehouse is null)
        {
            return Result<WarehouseResponse>.Failure(MastersErrorCodes.NotFound, $"Warehouse '{id}' was not found.");
        }

        warehouse.Update(request.WarehouseName, request.Address, request.Country, request.State, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WarehouseResponse>.Success(warehouse.ToResponse());
    }

    public async Task<Result<WarehouseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _repository.GetByIdAsync(id, cancellationToken);
        return warehouse is null
            ? Result<WarehouseResponse>.Failure(MastersErrorCodes.NotFound, $"Warehouse '{id}' was not found.")
            : Result<WarehouseResponse>.Success(warehouse.ToResponse());
    }

    public async Task<PagedResult<WarehouseResponse>> GetPagedAsync(WarehouseListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<WarehouseResponse>(items.Select(w => w.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
