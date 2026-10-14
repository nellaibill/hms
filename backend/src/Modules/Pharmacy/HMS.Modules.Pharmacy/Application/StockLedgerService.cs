using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
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

        var products = await _productService.GetLookupAsync(items.Select(t => t.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(items.Select(t => t.ProductBatchId), cancellationToken);
        var patientNames = await _patientService.GetNameLookupAsync(items.Select(t => t.PatientId), cancellationToken);

        var responses = items
            .Select(t => t.ToLedgerResponse(
                products.GetValueOrDefault(t.ProductId)?.ProductName ?? string.Empty,
                batches.GetValueOrDefault(t.ProductBatchId)?.BatchNo ?? string.Empty,
                t.PatientId is { } patientId ? patientNames.GetValueOrDefault(patientId) : null))
            .ToList();

        return new PagedResult<StockTransactionResponse>(responses, query.Page, query.PageSize, totalCount);
    }
}
