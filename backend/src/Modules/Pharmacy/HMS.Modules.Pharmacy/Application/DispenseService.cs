using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Products.Application;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): DispensesController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051). Mirrors HMS.Modules.IPD.Application.IAdmissionService.
/// </summary>
public interface IDispenseService
{
    Task<Result<DispenseResponse>> CreateAsync(CreateDispenseRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DispenseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DispenseResponse>> GetPagedAsync(DispenseListQuery query, CancellationToken cancellationToken);
}

internal class DispenseService : IDispenseService
{
    /// <summary>
    /// The lost-update race this guards against: two concurrent dispenses read the same
    /// QuantityOnHand, both pass the "enough stock" check, then both try to decrement — the
    /// loser's SaveChangesAsync throws DbUpdateConcurrencyException (the xmin token it read
    /// no longer matches). Instead of surfacing that as a raw 500, the loser re-fetches the
    /// now-fresh balance, re-validates quantity is no more than QuantityOnHand against it, and either
    /// succeeds against the winner's updated total or correctly fails InsufficientStock —
    /// bounded at 3 attempts so a pathological hot-batch can't retry forever.
    /// </summary>
    private const int MaxDispenseAttempts = 3;

    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IPharmacyStockTransactionRepository _transactionRepository;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly IPatientService _patientService;

    public DispenseService(
        IPharmacyStockBalanceRepository balanceRepository,
        IPharmacyStockTransactionRepository transactionRepository,
        IProductService productService,
        IProductBatchService productBatchService,
        IPatientService patientService)
    {
        _balanceRepository = balanceRepository;
        _transactionRepository = transactionRepository;
        _productService = productService;
        _productBatchService = productBatchService;
        _patientService = patientService;
    }

    public async Task<Result<DispenseResponse>> CreateAsync(CreateDispenseRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patientResult = await _patientService.GetByIdAsync(request.PatientId, cancellationToken);
        if (!patientResult.IsSuccess)
        {
            return Result<DispenseResponse>.Failure(PharmacyErrorCodes.InvalidPatient, $"Patient '{request.PatientId}' was not found.");
        }

        var productResult = await _productService.GetByIdAsync(request.ProductId, cancellationToken);
        if (!productResult.IsSuccess)
        {
            return Result<DispenseResponse>.Failure(PharmacyErrorCodes.InvalidProduct, $"Product '{request.ProductId}' was not found.");
        }

        var batchResult = await _productBatchService.GetByIdAsync(request.ProductId, request.ProductBatchId, cancellationToken);
        if (!batchResult.IsSuccess)
        {
            return Result<DispenseResponse>.Failure(PharmacyErrorCodes.InvalidBatch, $"Batch '{request.ProductBatchId}' was not found for product '{request.ProductId}'.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (batchResult.Value!.ExpiryDate < today)
        {
            return Result<DispenseResponse>.Failure(
                PharmacyErrorCodes.BatchExpired,
                $"Batch '{batchResult.Value.BatchNo}' expired on {batchResult.Value.ExpiryDate:yyyy-MM-dd}.");
        }

        PharmacyStockBalance? balance = null;

        for (var attempt = 1; attempt <= MaxDispenseAttempts; attempt++)
        {
            balance = await _balanceRepository.GetByProductAndBatchAsync(request.ProductId, request.ProductBatchId, cancellationToken);
            var quantityOnHand = balance?.QuantityOnHand ?? 0m;

            if (request.Quantity > quantityOnHand)
            {
                return Result<DispenseResponse>.Failure(
                    PharmacyErrorCodes.InsufficientStock,
                    $"Cannot dispense {request.Quantity} — only {quantityOnHand} available for this product/batch.");
            }

            balance!.Dispense(request.Quantity, actorId);

            try
            {
                await _balanceRepository.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxDispenseAttempts)
            {
                // A concurrent dispense against the same batch committed first. Loop again:
                // the next GetByProductAndBatchAsync re-fetches the now-current balance and
                // the quantity check above re-validates against it before we try again.
            }
        }

        var transaction = PharmacyStockTransaction.CreateDispense(
            request.ProductId,
            request.ProductBatchId,
            request.Quantity,
            balance!.QuantityOnHand,
            request.PatientId,
            request.AdmissionId,
            request.Remarks,
            actorId);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _balanceRepository.SaveChangesAsync(cancellationToken);

        var patient = patientResult.Value!;
        return Result<DispenseResponse>.Success(
            transaction.ToDispenseResponse(productResult.Value!.ProductName, batchResult.Value.BatchNo, $"{patient.FirstName} {patient.LastName}"));
    }

    public async Task<Result<DispenseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction is null || transaction.TransactionType != TransactionType.Dispense)
        {
            return Result<DispenseResponse>.Failure(PharmacyErrorCodes.NotFound, $"Dispense '{id}' was not found.");
        }

        return Result<DispenseResponse>.Success(await BuildResponseAsync(transaction, cancellationToken));
    }

    public async Task<PagedResult<DispenseResponse>> GetPagedAsync(DispenseListQuery query, CancellationToken cancellationToken)
    {
        var ledgerQuery = new StockLedgerListQuery
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Sort = query.Sort,
            ProductId = query.ProductId,
            PatientId = query.PatientId,
            TransactionType = TransactionType.Dispense,
        };

        var (items, totalCount) = await _transactionRepository.GetPagedAsync(ledgerQuery, cancellationToken);

        // MVP-scale N+1 lookups (one Product/ProductBatch/Patient round-trip per row) to
        // denormalize display fields — same trade-off IPD's AdmissionService.GetPagedAsync
        // documents; revisit with a bulk lookup if this list grows large enough to matter.
        var responses = new List<DispenseResponse>(items.Count);
        foreach (var item in items)
        {
            responses.Add(await BuildResponseAsync(item, cancellationToken));
        }

        return new PagedResult<DispenseResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private async Task<DispenseResponse> BuildResponseAsync(PharmacyStockTransaction transaction, CancellationToken cancellationToken)
    {
        var productResult = await _productService.GetByIdAsync(transaction.ProductId, cancellationToken);
        var batchResult = await _productBatchService.GetByIdAsync(transaction.ProductId, transaction.ProductBatchId, cancellationToken);

        var patientName = string.Empty;
        if (transaction.PatientId.HasValue)
        {
            var patientResult = await _patientService.GetByIdAsync(transaction.PatientId.Value, cancellationToken);
            var patient = patientResult.Value;
            patientName = patient is null ? string.Empty : $"{patient.FirstName} {patient.LastName}";
        }

        return transaction.ToDispenseResponse(
            productResult.Value?.ProductName ?? string.Empty,
            batchResult.Value?.BatchNo ?? string.Empty,
            patientName);
    }
}
