using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Patients.Application;
using HMS.Modules.Products.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): StockReceiptsController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051). Mirrors HMS.Modules.IPD.Application.IAdmissionService.
/// </summary>
public interface IStockReceiptService
{
    Task<Result<StockReceiptResponse>> CreateAsync(CreateStockReceiptRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockReceiptResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<StockReceiptResponse>> GetPagedAsync(StockReceiptListQuery query, CancellationToken cancellationToken);
}

internal class StockReceiptService : IStockReceiptService
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IPharmacyStockTransactionRepository _transactionRepository;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;

    public StockReceiptService(
        IPharmacyStockBalanceRepository balanceRepository,
        IPharmacyStockTransactionRepository transactionRepository,
        IProductService productService,
        IProductBatchService productBatchService)
    {
        _balanceRepository = balanceRepository;
        _transactionRepository = transactionRepository;
        _productService = productService;
        _productBatchService = productBatchService;
    }

    public async Task<Result<StockReceiptResponse>> CreateAsync(CreateStockReceiptRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var productResult = await _productService.GetByIdAsync(request.ProductId, cancellationToken);
        if (!productResult.IsSuccess)
        {
            return Result<StockReceiptResponse>.Failure(PharmacyErrorCodes.InvalidProduct, $"Product '{request.ProductId}' was not found.");
        }

        var batchResult = await _productBatchService.GetByIdAsync(request.ProductId, request.ProductBatchId, cancellationToken);
        if (!batchResult.IsSuccess)
        {
            return Result<StockReceiptResponse>.Failure(PharmacyErrorCodes.InvalidBatch, $"Batch '{request.ProductBatchId}' was not found for product '{request.ProductId}'.");
        }

        var balance = await _balanceRepository.GetByProductAndBatchAsync(request.ProductId, request.ProductBatchId, cancellationToken);
        if (balance is null)
        {
            balance = PharmacyStockBalance.Create(request.ProductId, request.ProductBatchId, actorId);
            await _balanceRepository.AddAsync(balance, cancellationToken);
        }

        balance.Receive(request.Quantity, actorId);

        var transaction = PharmacyStockTransaction.CreateReceipt(
            request.ProductId,
            request.ProductBatchId,
            request.Quantity,
            balance.QuantityOnHand,
            request.Remarks,
            actorId);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _balanceRepository.SaveChangesAsync(cancellationToken);

        return Result<StockReceiptResponse>.Success(
            transaction.ToReceiptResponse(productResult.Value!.ProductName, batchResult.Value!.BatchNo));
    }

    public async Task<Result<StockReceiptResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction is null || transaction.TransactionType != TransactionType.Receipt)
        {
            return Result<StockReceiptResponse>.Failure(PharmacyErrorCodes.NotFound, $"Stock receipt '{id}' was not found.");
        }

        return Result<StockReceiptResponse>.Success(await BuildResponseAsync(transaction, cancellationToken));
    }

    public async Task<PagedResult<StockReceiptResponse>> GetPagedAsync(StockReceiptListQuery query, CancellationToken cancellationToken)
    {
        var ledgerQuery = new StockLedgerListQuery
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Sort = query.Sort,
            ProductId = query.ProductId,
            TransactionType = TransactionType.Receipt,
        };

        var (items, totalCount) = await _transactionRepository.GetPagedAsync(ledgerQuery, cancellationToken);

        // MVP-scale N+1 lookups (one Product/ProductBatch round-trip per row) to denormalize
        // display fields — acceptable at Pharmacy's current volumes; revisit with a bulk
        // lookup if this list grows large enough to matter (same trade-off IPD's
        // AdmissionService.GetPagedAsync documents).
        var responses = new List<StockReceiptResponse>(items.Count);
        foreach (var item in items)
        {
            responses.Add(await BuildResponseAsync(item, cancellationToken));
        }

        return new PagedResult<StockReceiptResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private async Task<StockReceiptResponse> BuildResponseAsync(PharmacyStockTransaction transaction, CancellationToken cancellationToken)
    {
        var productResult = await _productService.GetByIdAsync(transaction.ProductId, cancellationToken);
        var batchResult = await _productBatchService.GetByIdAsync(transaction.ProductId, transaction.ProductBatchId, cancellationToken);

        return transaction.ToReceiptResponse(
            productResult.Value?.ProductName ?? string.Empty,
            batchResult.Value?.BatchNo ?? string.Empty);
    }
}
