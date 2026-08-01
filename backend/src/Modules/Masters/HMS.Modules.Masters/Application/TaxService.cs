using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface ITaxService
{
    Task<Result<TaxResponse>> CreateAsync(CreateTaxRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<TaxResponse>> UpdateAsync(Guid id, UpdateTaxRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<TaxResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TaxResponse>> GetPagedAsync(TaxListQuery query, CancellationToken cancellationToken);
}

internal class TaxService : ITaxService
{
    private readonly ITaxRepository _repository;

    public TaxService(ITaxRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaxResponse>> CreateAsync(CreateTaxRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.TaxCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<TaxResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Tax code '{request.TaxCode}' is already in use.");
        }

        var tax = Tax.Create(request.TaxCode, request.TaxName, request.TaxType, request.RatePercent, request.IsInclusive, request.IsActive, actorId);

        await _repository.AddAsync(tax, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<TaxResponse>.Success(tax.ToResponse());
    }

    public async Task<Result<TaxResponse>> UpdateAsync(Guid id, UpdateTaxRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var tax = await _repository.GetByIdAsync(id, cancellationToken);
        if (tax is null)
        {
            return Result<TaxResponse>.Failure(MastersErrorCodes.NotFound, $"Tax '{id}' was not found.");
        }

        tax.Update(request.TaxName, request.TaxType, request.RatePercent, request.IsInclusive, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<TaxResponse>.Success(tax.ToResponse());
    }

    public async Task<Result<TaxResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tax = await _repository.GetByIdAsync(id, cancellationToken);
        return tax is null
            ? Result<TaxResponse>.Failure(MastersErrorCodes.NotFound, $"Tax '{id}' was not found.")
            : Result<TaxResponse>.Success(tax.ToResponse());
    }

    public async Task<PagedResult<TaxResponse>> GetPagedAsync(TaxListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<TaxResponse>(items.Select(t => t.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
