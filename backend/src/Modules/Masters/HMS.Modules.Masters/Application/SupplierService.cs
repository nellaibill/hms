using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface ISupplierService
{
    Task<Result<SupplierResponse>> CreateAsync(CreateSupplierRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SupplierResponse>> UpdateAsync(Guid id, UpdateSupplierRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SupplierResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SupplierResponse>> GetPagedAsync(SupplierListQuery query, CancellationToken cancellationToken);
}

internal class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;
    private readonly IPaymentTermRepository _paymentTermRepository;

    public SupplierService(ISupplierRepository repository, IPaymentTermRepository paymentTermRepository)
    {
        _repository = repository;
        _paymentTermRepository = paymentTermRepository;
    }

    public async Task<Result<SupplierResponse>> CreateAsync(CreateSupplierRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.SupplierCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<SupplierResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Supplier code '{request.SupplierCode}' is already in use.");
        }

        if (request.PaymentTermId.HasValue && await _paymentTermRepository.GetByIdAsync(request.PaymentTermId.Value, cancellationToken) is null)
        {
            return Result<SupplierResponse>.Failure(MastersErrorCodes.InvalidReference, $"Payment term '{request.PaymentTermId}' was not found.");
        }

        var supplier = Supplier.Create(request.SupplierCode, request.SupplierName, request.ContactPerson, request.Phone, request.Email, request.TaxId, request.Country, request.PaymentTermId, request.IsActive, actorId);

        await _repository.AddAsync(supplier, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<SupplierResponse>.Success(supplier.ToResponse());
    }

    public async Task<Result<SupplierResponse>> UpdateAsync(Guid id, UpdateSupplierRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return Result<SupplierResponse>.Failure(MastersErrorCodes.NotFound, $"Supplier '{id}' was not found.");
        }

        if (request.PaymentTermId.HasValue && await _paymentTermRepository.GetByIdAsync(request.PaymentTermId.Value, cancellationToken) is null)
        {
            return Result<SupplierResponse>.Failure(MastersErrorCodes.InvalidReference, $"Payment term '{request.PaymentTermId}' was not found.");
        }

        supplier.Update(request.SupplierName, request.ContactPerson, request.Phone, request.Email, request.TaxId, request.Country, request.PaymentTermId, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<SupplierResponse>.Success(supplier.ToResponse());
    }

    public async Task<Result<SupplierResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(id, cancellationToken);
        return supplier is null
            ? Result<SupplierResponse>.Failure(MastersErrorCodes.NotFound, $"Supplier '{id}' was not found.")
            : Result<SupplierResponse>.Success(supplier.ToResponse());
    }

    public async Task<PagedResult<SupplierResponse>> GetPagedAsync(SupplierListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<SupplierResponse>(items.Select(s => s.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
