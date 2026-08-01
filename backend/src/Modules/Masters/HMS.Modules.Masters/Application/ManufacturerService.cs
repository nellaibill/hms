using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IManufacturerService
{
    Task<Result<ManufacturerResponse>> CreateAsync(CreateManufacturerRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ManufacturerResponse>> UpdateAsync(Guid id, UpdateManufacturerRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ManufacturerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ManufacturerResponse>> GetPagedAsync(ManufacturerListQuery query, CancellationToken cancellationToken);
}

internal class ManufacturerService : IManufacturerService
{
    private readonly IManufacturerRepository _repository;

    public ManufacturerService(IManufacturerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ManufacturerResponse>> CreateAsync(CreateManufacturerRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.ManufacturerCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ManufacturerResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Manufacturer code '{request.ManufacturerCode}' is already in use.");
        }

        var manufacturer = Manufacturer.Create(request.ManufacturerCode, request.ManufacturerName, request.ContactPerson, request.Phone, request.Email, request.Country, request.IsActive, actorId);

        await _repository.AddAsync(manufacturer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ManufacturerResponse>.Success(manufacturer.ToResponse());
    }

    public async Task<Result<ManufacturerResponse>> UpdateAsync(Guid id, UpdateManufacturerRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var manufacturer = await _repository.GetByIdAsync(id, cancellationToken);
        if (manufacturer is null)
        {
            return Result<ManufacturerResponse>.Failure(MastersErrorCodes.NotFound, $"Manufacturer '{id}' was not found.");
        }

        manufacturer.Update(request.ManufacturerName, request.ContactPerson, request.Phone, request.Email, request.Country, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ManufacturerResponse>.Success(manufacturer.ToResponse());
    }

    public async Task<Result<ManufacturerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var manufacturer = await _repository.GetByIdAsync(id, cancellationToken);
        return manufacturer is null
            ? Result<ManufacturerResponse>.Failure(MastersErrorCodes.NotFound, $"Manufacturer '{id}' was not found.")
            : Result<ManufacturerResponse>.Success(manufacturer.ToResponse());
    }

    public async Task<PagedResult<ManufacturerResponse>> GetPagedAsync(ManufacturerListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ManufacturerResponse>(items.Select(m => m.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
