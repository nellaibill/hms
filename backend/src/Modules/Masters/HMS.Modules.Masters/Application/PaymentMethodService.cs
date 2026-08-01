using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IPaymentMethodService
{
    Task<Result<PaymentMethodResponse>> CreateAsync(CreatePaymentMethodRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PaymentMethodResponse>> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PaymentMethodResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PaymentMethodResponse>> GetPagedAsync(PaymentMethodListQuery query, CancellationToken cancellationToken);
}

internal class PaymentMethodService : IPaymentMethodService
{
    private readonly IPaymentMethodRepository _repository;

    public PaymentMethodService(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaymentMethodResponse>> CreateAsync(CreatePaymentMethodRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.MethodCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<PaymentMethodResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Payment method code '{request.MethodCode}' is already in use.");
        }

        var method = PaymentMethod.Create(request.MethodCode, request.MethodName, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(method, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<PaymentMethodResponse>.Success(method.ToResponse());
    }

    public async Task<Result<PaymentMethodResponse>> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var method = await _repository.GetByIdAsync(id, cancellationToken);
        if (method is null)
        {
            return Result<PaymentMethodResponse>.Failure(MastersErrorCodes.NotFound, $"Payment method '{id}' was not found.");
        }

        method.Update(request.MethodName, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<PaymentMethodResponse>.Success(method.ToResponse());
    }

    public async Task<Result<PaymentMethodResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var method = await _repository.GetByIdAsync(id, cancellationToken);
        return method is null
            ? Result<PaymentMethodResponse>.Failure(MastersErrorCodes.NotFound, $"Payment method '{id}' was not found.")
            : Result<PaymentMethodResponse>.Success(method.ToResponse());
    }

    public async Task<PagedResult<PaymentMethodResponse>> GetPagedAsync(PaymentMethodListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<PaymentMethodResponse>(items.Select(p => p.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
