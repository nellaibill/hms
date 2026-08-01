using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IUnitConversionService
{
    Task<Result<UnitConversionResponse>> CreateAsync(CreateUnitConversionRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UnitConversionResponse>> UpdateAsync(Guid id, UpdateUnitConversionRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<UnitConversionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<UnitConversionResponse>> GetPagedAsync(UnitConversionListQuery query, CancellationToken cancellationToken);
}

internal class UnitConversionService : IUnitConversionService
{
    private readonly IUnitConversionRepository _repository;
    private readonly IUnitOfMeasureRepository _uomRepository;

    public UnitConversionService(IUnitConversionRepository repository, IUnitOfMeasureRepository uomRepository)
    {
        _repository = repository;
        _uomRepository = uomRepository;
    }

    public async Task<Result<UnitConversionResponse>> CreateAsync(CreateUnitConversionRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _uomRepository.GetByIdAsync(request.FromUomId, cancellationToken) is null)
        {
            return Result<UnitConversionResponse>.Failure(MastersErrorCodes.InvalidReference, $"Unit of measure '{request.FromUomId}' was not found.");
        }

        if (await _uomRepository.GetByIdAsync(request.ToUomId, cancellationToken) is null)
        {
            return Result<UnitConversionResponse>.Failure(MastersErrorCodes.InvalidReference, $"Unit of measure '{request.ToUomId}' was not found.");
        }

        if (await _repository.ExistsAsync(request.FromUomId, request.ToUomId, excludingId: null, cancellationToken))
        {
            return Result<UnitConversionResponse>.Failure(MastersErrorCodes.DuplicateCode, "A conversion between these two units already exists.");
        }

        var conversion = UnitConversion.Create(request.FromUomId, request.ToUomId, request.ConversionFactor, request.IsActive, actorId);

        await _repository.AddAsync(conversion, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<UnitConversionResponse>.Success(conversion.ToResponse());
    }

    public async Task<Result<UnitConversionResponse>> UpdateAsync(Guid id, UpdateUnitConversionRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var conversion = await _repository.GetByIdAsync(id, cancellationToken);
        if (conversion is null)
        {
            return Result<UnitConversionResponse>.Failure(MastersErrorCodes.NotFound, $"Unit conversion '{id}' was not found.");
        }

        conversion.Update(request.ConversionFactor, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<UnitConversionResponse>.Success(conversion.ToResponse());
    }

    public async Task<Result<UnitConversionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversion = await _repository.GetByIdAsync(id, cancellationToken);
        return conversion is null
            ? Result<UnitConversionResponse>.Failure(MastersErrorCodes.NotFound, $"Unit conversion '{id}' was not found.")
            : Result<UnitConversionResponse>.Success(conversion.ToResponse());
    }

    public async Task<PagedResult<UnitConversionResponse>> GetPagedAsync(UnitConversionListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<UnitConversionResponse>(items.Select(u => u.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
