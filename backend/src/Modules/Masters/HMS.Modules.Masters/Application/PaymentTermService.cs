using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IPaymentTermService
{
    Task<Result<PaymentTermResponse>> CreateAsync(CreatePaymentTermRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PaymentTermResponse>> UpdateAsync(Guid id, UpdatePaymentTermRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PaymentTermResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PaymentTermResponse>> GetPagedAsync(PaymentTermListQuery query, CancellationToken cancellationToken);
}

internal class PaymentTermService : IPaymentTermService
{
    private readonly IPaymentTermRepository _repository;

    public PaymentTermService(IPaymentTermRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaymentTermResponse>> CreateAsync(CreatePaymentTermRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByNameAsync(request.TermName.Trim(), excludingId: null, cancellationToken))
        {
            return Result<PaymentTermResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Payment term '{request.TermName}' is already in use.");
        }

        var term = PaymentTerm.Create(request.TermName, request.Days, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(term, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<PaymentTermResponse>.Success(term.ToResponse());
    }

    public async Task<Result<PaymentTermResponse>> UpdateAsync(Guid id, UpdatePaymentTermRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var term = await _repository.GetByIdAsync(id, cancellationToken);
        if (term is null)
        {
            return Result<PaymentTermResponse>.Failure(MastersErrorCodes.NotFound, $"Payment term '{id}' was not found.");
        }

        if (await _repository.ExistsByNameAsync(request.TermName.Trim(), excludingId: id, cancellationToken))
        {
            return Result<PaymentTermResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Payment term '{request.TermName}' is already in use.");
        }

        term.Update(request.TermName, request.Days, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<PaymentTermResponse>.Success(term.ToResponse());
    }

    public async Task<Result<PaymentTermResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var term = await _repository.GetByIdAsync(id, cancellationToken);
        return term is null
            ? Result<PaymentTermResponse>.Failure(MastersErrorCodes.NotFound, $"Payment term '{id}' was not found.")
            : Result<PaymentTermResponse>.Success(term.ToResponse());
    }

    public async Task<PagedResult<PaymentTermResponse>> GetPagedAsync(PaymentTermListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<PaymentTermResponse>(items.Select(p => p.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
