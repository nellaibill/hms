using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IUnitOfMeasureService
{
    Task<Result<UnitOfMeasureResponse>> CreateAsync(CreateUnitOfMeasureRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UnitOfMeasureResponse>> UpdateAsync(Guid id, UpdateUnitOfMeasureRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UnitOfMeasureResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<UnitOfMeasureResponse>> GetPagedAsync(UnitOfMeasureListQuery query, CancellationToken cancellationToken);
}

internal class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly IUnitOfMeasureRepository _repository;

    public UnitOfMeasureService(IUnitOfMeasureRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UnitOfMeasureResponse>> CreateAsync(CreateUnitOfMeasureRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.UomCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<UnitOfMeasureResponse>.Failure(MastersErrorCodes.DuplicateCode, $"UOM code '{request.UomCode}' is already in use.");
        }

        var uom = UnitOfMeasure.Create(request.UomCode, request.UomName, request.UomType, request.IsBaseUnit, request.IsActive, actorId);

        await _repository.AddAsync(uom, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<UnitOfMeasureResponse>.Success(uom.ToResponse());
    }

    public async Task<Result<UnitOfMeasureResponse>> UpdateAsync(Guid id, UpdateUnitOfMeasureRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var uom = await _repository.GetByIdAsync(id, cancellationToken);
        if (uom is null)
        {
            return Result<UnitOfMeasureResponse>.Failure(MastersErrorCodes.NotFound, $"Unit of measure '{id}' was not found.");
        }

        uom.Update(request.UomName, request.UomType, request.IsBaseUnit, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<UnitOfMeasureResponse>.Success(uom.ToResponse());
    }

    public async Task<Result<UnitOfMeasureResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var uom = await _repository.GetByIdAsync(id, cancellationToken);
        return uom is null
            ? Result<UnitOfMeasureResponse>.Failure(MastersErrorCodes.NotFound, $"Unit of measure '{id}' was not found.")
            : Result<UnitOfMeasureResponse>.Success(uom.ToResponse());
    }

    public async Task<PagedResult<UnitOfMeasureResponse>> GetPagedAsync(UnitOfMeasureListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<UnitOfMeasureResponse>(items.Select(u => u.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
