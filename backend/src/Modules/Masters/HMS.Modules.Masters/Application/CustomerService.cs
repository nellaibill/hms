using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface ICustomerService
{
    Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CustomerResponse>> GetPagedAsync(CustomerListQuery query, CancellationToken cancellationToken);
}

internal class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IPaymentTermRepository _paymentTermRepository;

    public CustomerService(ICustomerRepository repository, IPaymentTermRepository paymentTermRepository)
    {
        _repository = repository;
        _paymentTermRepository = paymentTermRepository;
    }

    public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.CustomerCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<CustomerResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Customer code '{request.CustomerCode}' is already in use.");
        }

        if (request.PaymentTermId.HasValue && await _paymentTermRepository.GetByIdAsync(request.PaymentTermId.Value, cancellationToken) is null)
        {
            return Result<CustomerResponse>.Failure(MastersErrorCodes.InvalidReference, $"Payment term '{request.PaymentTermId}' was not found.");
        }

        var customer = Customer.Create(request.CustomerCode, request.CustomerName, request.ContactPerson, request.Phone, request.Email, request.Country, request.PaymentTermId, request.IsActive, actorId);

        await _repository.AddAsync(customer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<CustomerResponse>.Success(customer.ToResponse());
    }

    public async Task<Result<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result<CustomerResponse>.Failure(MastersErrorCodes.NotFound, $"Customer '{id}' was not found.");
        }

        if (request.PaymentTermId.HasValue && await _paymentTermRepository.GetByIdAsync(request.PaymentTermId.Value, cancellationToken) is null)
        {
            return Result<CustomerResponse>.Failure(MastersErrorCodes.InvalidReference, $"Payment term '{request.PaymentTermId}' was not found.");
        }

        customer.Update(request.CustomerName, request.ContactPerson, request.Phone, request.Email, request.Country, request.PaymentTermId, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<CustomerResponse>.Success(customer.ToResponse());
    }

    public async Task<Result<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);
        return customer is null
            ? Result<CustomerResponse>.Failure(MastersErrorCodes.NotFound, $"Customer '{id}' was not found.")
            : Result<CustomerResponse>.Success(customer.ToResponse());
    }

    public async Task<PagedResult<CustomerResponse>> GetPagedAsync(CustomerListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<CustomerResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
