using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Products.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): StockLedgerController takes this as a constructor dependency —
/// same CS0051 reason as IStockReceiptService.
/// </summary>
public interface IStockLedgerService
{
    Task<PagedResult<StockTransactionResponse>> GetPagedAsync(StockLedgerListQuery query, CancellationToken cancellationToken);
}

internal class StockLedgerService : IStockLedgerService
{
    private readonly IPharmacyStockTransactionRepository _transactionRepository;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly IPatientService _patientService;

    public StockLedgerService(
        IPharmacyStockTransactionRepository transactionRepository,
        IProductService productService,
        IProductBatchService productBatchService,
        IPatientService patientService)
    {
        _transactionRepository = transactionRepository;
        _productService = productService;
        _productBatchService = productBatchService;
        _patientService = patientService;
    }

    public async Task<PagedResult<StockTransactionResponse>> GetPagedAsync(StockLedgerListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _transactionRepository.GetPagedAsync(query, cancellationToken);

        // MVP-scale N+1 lookups (one Product/ProductBatch/Patient round-trip per row) to
        // denormalize display fields — same trade-off IPD's AdmissionService.GetPagedAsync
        // documents; revisit with a bulk lookup if this list grows large enough to matter.
        var responses = new List<StockTransactionResponse>(items.Count);
        foreach (var item in items)
        {
            responses.Add(await BuildResponseAsync(item, cancellationToken));
        }

        return new PagedResult<StockTransactionResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private async Task<StockTransactionResponse> BuildResponseAsync(PharmacyStockTransaction transaction, CancellationToken cancellationToken)
    {
        var productResult = await _productService.GetByIdAsync(transaction.ProductId, cancellationToken);
        var batchResult = await _productBatchService.GetByIdAsync(transaction.ProductId, transaction.ProductBatchId, cancellationToken);

        string? patientName = null;
        if (transaction.PatientId.HasValue)
        {
            var patientResult = await _patientService.GetByIdAsync(transaction.PatientId.Value, cancellationToken);
            var patient = patientResult.Value;
            patientName = patient is null ? null : $"{patient.FirstName} {patient.LastName}";
        }

        return transaction.ToLedgerResponse(
            productResult.Value?.ProductName ?? string.Empty,
            batchResult.Value?.BatchNo ?? string.Empty,
            patientName);
    }
}
