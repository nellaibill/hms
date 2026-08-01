using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): the one Application-layer type CurrenciesController takes as a
/// constructor dependency — mirrors HMS.Modules.Patients.IPatientService.
/// </summary>
public interface ICurrencyService
{
    Task<Result<CurrencyResponse>> CreateAsync(CreateCurrencyRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<CurrencyResponse>> UpdateAsync(Guid id, UpdateCurrencyRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<CurrencyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CurrencyResponse>> GetPagedAsync(CurrencyListQuery query, CancellationToken cancellationToken);
}

internal class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _repository;

    public CurrencyService(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CurrencyResponse>> CreateAsync(CreateCurrencyRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.CurrencyCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<CurrencyResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Currency code '{request.CurrencyCode}' is already in use.");
        }

        var currency = Currency.Create(request.CurrencyCode, request.CurrencyName, request.Symbol, request.DecimalPlaces, request.IsActive, actorId);

        await _repository.AddAsync(currency, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<CurrencyResponse>.Success(currency.ToResponse());
    }

    public async Task<Result<CurrencyResponse>> UpdateAsync(Guid id, UpdateCurrencyRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(id, cancellationToken);
        if (currency is null)
        {
            return Result<CurrencyResponse>.Failure(MastersErrorCodes.NotFound, $"Currency '{id}' was not found.");
        }

        currency.Update(request.CurrencyName, request.Symbol, request.DecimalPlaces, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<CurrencyResponse>.Success(currency.ToResponse());
    }

    public async Task<Result<CurrencyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var currency = await _repository.GetByIdAsync(id, cancellationToken);
        return currency is null
            ? Result<CurrencyResponse>.Failure(MastersErrorCodes.NotFound, $"Currency '{id}' was not found.")
            : Result<CurrencyResponse>.Success(currency.ToResponse());
    }

    public async Task<PagedResult<CurrencyResponse>> GetPagedAsync(CurrencyListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<CurrencyResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
