using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): StockBalancesController takes this as a constructor dependency —
/// same CS0051 reason as IStockReceiptService.
/// </summary>
public interface IStockBalanceService
{
    Task<Result<StockBalanceResponse>> GetAsync(Guid productId, Guid productBatchId, CancellationToken cancellationToken);

    Task<PagedResult<StockBalanceResponse>> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken);
}

internal class StockBalanceService : IStockBalanceService
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;

    public StockBalanceService(
        IPharmacyStockBalanceRepository balanceRepository,
        IProductService productService,
        IProductBatchService productBatchService)
    {
        _balanceRepository = balanceRepository;
        _productService = productService;
        _productBatchService = productBatchService;
    }

    public async Task<Result<StockBalanceResponse>> GetAsync(Guid productId, Guid productBatchId, CancellationToken cancellationToken)
    {
        var balance = await _balanceRepository.GetByProductAndBatchAsync(productId, productBatchId, cancellationToken);
        if (balance is null)
        {
            return Result<StockBalanceResponse>.Failure(
                PharmacyErrorCodes.NotFound,
                $"No stock balance found for product '{productId}' / batch '{productBatchId}'.");
        }

        return Result<StockBalanceResponse>.Success(await BuildResponseAsync(balance, cancellationToken));
    }

    public async Task<PagedResult<StockBalanceResponse>> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _balanceRepository.GetPagedAsync(query, cancellationToken);

        // MVP-scale N+1 lookups (one Product/ProductBatch round-trip per row) to denormalize
        // display fields — same trade-off IPD's AdmissionService.GetPagedAsync documents.
        var responses = new List<StockBalanceResponse>(items.Count);
        foreach (var item in items)
        {
            responses.Add(await BuildResponseAsync(item, cancellationToken));
        }

        return new PagedResult<StockBalanceResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private async Task<StockBalanceResponse> BuildResponseAsync(PharmacyStockBalance balance, CancellationToken cancellationToken)
    {
        var productResult = await _productService.GetByIdAsync(balance.ProductId, cancellationToken);
        var batchResult = await _productBatchService.GetByIdAsync(balance.ProductId, balance.ProductBatchId, cancellationToken);

        return balance.ToResponse(
            productResult.Value?.ProductName ?? string.Empty,
            batchResult.Value?.BatchNo ?? string.Empty,
            batchResult.Value?.ExpiryDate ?? default);
    }
}
