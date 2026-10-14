using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): StoresController takes this as a constructor dependency — same
/// CS0051 reason as IStockReceiptService.
/// </summary>
public interface IStoreService
{
    Task<IReadOnlyList<StoreResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<Result<StoreResponse>> CreateAsync(CreateStoreRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StoreResponse>> UpdateAsync(Guid id, UpdateStoreRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class StoreService : IStoreService
{
    private readonly IPharmacyStoreRepository _repository;

    public StoreService(IPharmacyStoreRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StoreResponse>> GetAllAsync(CancellationToken cancellationToken)
        => (await _repository.GetAllAsync(cancellationToken)).Select(ToResponse).ToList();

    public async Task<Result<StoreResponse>> CreateAsync(CreateStoreRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), cancellationToken))
        {
            return Result<StoreResponse>.Failure(PharmacyErrorCodes.DuplicateStoreCode, $"Store code '{request.Code}' is already in use.");
        }

        var store = PharmacyStore.Create(request.Code, request.Name, request.StoreType, actorId);
        await _repository.AddAsync(store, cancellationToken);

        // The very first store has to be the default whatever the request says — otherwise a
        // stock request that names no store would have nowhere to go.
        var currentDefault = await _repository.GetDefaultAsync(cancellationToken);
        if (request.IsDefault || currentDefault is null)
        {
            currentDefault?.ClearDefault(actorId);
            store.MakeDefault(actorId);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result<StoreResponse>.Success(ToResponse(store));
    }

    public async Task<Result<StoreResponse>> UpdateAsync(Guid id, UpdateStoreRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _repository.GetByIdAsync(id, cancellationToken);
        if (store is null)
        {
            return Result<StoreResponse>.Failure(PharmacyErrorCodes.NotFound, $"Store '{id}' was not found.");
        }

        if (!request.IsActive && (store.IsDefault || request.IsDefault))
        {
            return Result<StoreResponse>.Failure(
                PharmacyErrorCodes.DefaultStoreRequired,
                "The default store cannot be deactivated. Make another store the default first.");
        }

        store.Update(request.Name, request.StoreType, request.IsActive, actorId);

        if (request.IsDefault && !store.IsDefault)
        {
            var currentDefault = await _repository.GetDefaultAsync(cancellationToken);
            currentDefault?.ClearDefault(actorId);
            store.MakeDefault(actorId);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result<StoreResponse>.Success(ToResponse(store));
    }

    private static StoreResponse ToResponse(PharmacyStore store) => new()
    {
        Id = store.Id,
        Code = store.Code,
        Name = store.Name,
        StoreType = store.StoreType,
        IsDefault = store.IsDefault,
        IsActive = store.IsActive,
        CreatedAt = store.CreatedAt,
        UpdatedAt = store.UpdatedAt,
    };
}
