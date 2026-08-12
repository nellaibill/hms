using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): BedsController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as
/// a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IBedService
{
    Task<Result<BedResponse>> CreateAsync(CreateBedRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<BedResponse>> UpdateAsync(Guid id, UpdateBedRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<BedResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<BedResponse>> GetPagedAsync(BedListQuery query, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<BedResponse>>> GetAvailableAsync(Guid? wardId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class BedService : IBedService
{
    private readonly IBedRepository _repository;
    private readonly IWardRepository _wardRepository;

    public BedService(IBedRepository repository, IWardRepository wardRepository)
    {
        _repository = repository;
        _wardRepository = wardRepository;
    }

    public async Task<Result<BedResponse>> CreateAsync(CreateBedRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _wardRepository.GetByIdAsync(request.WardId, cancellationToken) is null)
        {
            return Result<BedResponse>.Failure(IPDErrorCodes.InvalidWard, $"Ward '{request.WardId}' was not found.");
        }

        if (await _repository.ExistsByBedNumberAsync(request.WardId, request.BedNumber.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<BedResponse>.Failure(IPDErrorCodes.DuplicateBedNumber, $"Bed number '{request.BedNumber}' is already in use within this ward.");
        }

        // See UpdateAsync — Occupied must only ever be set by AdmissionService, never picked
        // directly, or the bed ends up "occupied" with no admission behind it.
        if (request.Status == BedStatus.Occupied)
        {
            return Result<BedResponse>.Failure(IPDErrorCodes.BedOccupied, "A new bed cannot be created as Occupied. Admit a patient through the New Admission workflow instead.");
        }

        var bed = Bed.Create(request.WardId, request.BedNumber, request.BedType, request.Status, request.IsActive, actorId);

        await _repository.AddAsync(bed, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<BedResponse>.Success(bed.ToResponse());
    }

    public async Task<Result<BedResponse>> UpdateAsync(Guid id, UpdateBedRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var bed = await _repository.GetByIdAsync(id, cancellationToken);
        if (bed is null)
        {
            return Result<BedResponse>.Failure(IPDErrorCodes.NotFound, $"Bed '{id}' was not found.");
        }

        // Occupied is driven exclusively by AdmissionService (admit/transfer/discharge) — it
        // reflects a real patient in the bed, so this generic edit endpoint must not be able
        // to move a bed into or out of that state and desync it from the actual admission.
        if (bed.Status == BedStatus.Occupied && request.Status != BedStatus.Occupied)
        {
            return Result<BedResponse>.Failure(IPDErrorCodes.BedOccupied, "This bed is occupied by an admitted patient. Discharge or transfer the patient before changing its status.");
        }

        if (bed.Status != BedStatus.Occupied && request.Status == BedStatus.Occupied)
        {
            return Result<BedResponse>.Failure(IPDErrorCodes.BedOccupied, "Bed status cannot be set to Occupied directly. Admit a patient through the New Admission workflow instead.");
        }

        bed.Update(request.BedType, request.Status, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<BedResponse>.Success(bed.ToResponse());
    }

    public async Task<Result<BedResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var bed = await _repository.GetByIdAsync(id, cancellationToken);
        return bed is null
            ? Result<BedResponse>.Failure(IPDErrorCodes.NotFound, $"Bed '{id}' was not found.")
            : Result<BedResponse>.Success(bed.ToResponse());
    }

    public async Task<PagedResult<BedResponse>> GetPagedAsync(BedListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<BedResponse>(items.Select(b => b.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<IReadOnlyList<BedResponse>>> GetAvailableAsync(Guid? wardId, CancellationToken cancellationToken)
    {
        var beds = await _repository.GetAvailableAsync(wardId, cancellationToken);
        return Result<IReadOnlyList<BedResponse>>.Success(beds.Select(b => b.ToResponse()).ToList());
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var bed = await _repository.GetByIdAsync(id, cancellationToken);
        if (bed is null)
        {
            return Result.Failure(IPDErrorCodes.NotFound, $"Bed '{id}' was not found.");
        }

        if (bed.Status == BedStatus.Occupied)
        {
            return Result.Failure(IPDErrorCodes.BedOccupied, "An occupied bed cannot be deleted. Discharge or transfer the patient first.");
        }

        bed.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
