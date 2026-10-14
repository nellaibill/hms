using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Mapping;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>
/// Public (not internal): StockBalancesController takes this as a constructor dependency —
/// same CS0051 reason as IStockReceiptService.
/// </summary>
public interface IStockBalanceService
{
    Task<Result<StockBalanceResponse>> GetAsync(Guid productId, Guid productBatchId, Guid? storeId, CancellationToken cancellationToken);

    Task<PagedResult<StockBalanceResponse>> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken);

    /// <summary>Stock per product (all its batches rolled up), name-ordered and searchable,
    /// optionally narrowed to the products needing attention.</summary>
    Task<PagedResult<StockSummaryResponse>> GetSummaryPagedAsync(StockSummaryListQuery query, CancellationToken cancellationToken);

    /// <summary>Every product matching one of the "needs attention" views, unpaged — for
    /// callers that act on the whole list (the reorder list, the expiry report).</summary>
    Task<IReadOnlyList<StockSummaryResponse>> GetSummariesAsync(StockSummaryFilter filter, Guid? storeId, CancellationToken cancellationToken);

    /// <summary>The batches of a product a dispense can draw on right now in one store (the
    /// default store when none is named), earliest expiry first, each with its unit price.</summary>
    Task<IReadOnlyList<AvailableBatchResponse>> GetAvailableBatchesAsync(Guid productId, Guid? storeId, CancellationToken cancellationToken);
}

internal class StockBalanceService : IStockBalanceService
{
    private readonly IPharmacyStockBalanceRepository _balanceRepository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly IPharmacySettingsRepository _settingsRepository;

    public StockBalanceService(
        IPharmacyStockBalanceRepository balanceRepository,
        IPharmacyStoreRepository storeRepository,
        IProductService productService,
        IProductBatchService productBatchService,
        IPharmacySettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        _balanceRepository = balanceRepository;
        _storeRepository = storeRepository;
        _productService = productService;
        _productBatchService = productBatchService;
    }

    public async Task<Result<StockBalanceResponse>> GetAsync(Guid productId, Guid productBatchId, Guid? storeId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        var balance = store is null ? null : await _balanceRepository.GetAsync(store.Id, productId, productBatchId, cancellationToken);
        if (balance is null)
        {
            return Result<StockBalanceResponse>.Failure(
                PharmacyErrorCodes.NotFound,
                $"No stock balance found for product '{productId}' / batch '{productBatchId}'.");
        }

        var productResult = await _productService.GetByIdAsync(balance.ProductId, cancellationToken);
        var batchResult = await _productBatchService.GetByIdAsync(balance.ProductId, balance.ProductBatchId, cancellationToken);

        return Result<StockBalanceResponse>.Success(balance.ToResponse(productResult.Value, batchResult.Value));
    }

    public async Task<PagedResult<StockBalanceResponse>> GetPagedAsync(StockBalanceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _balanceRepository.GetPagedAsync(query, cancellationToken);

        var products = await _productService.GetLookupAsync(items.Select(b => b.ProductId), cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(items.Select(b => b.ProductBatchId), cancellationToken);

        var responses = items
            .Select(b => b.ToResponse(products.GetValueOrDefault(b.ProductId), batches.GetValueOrDefault(b.ProductBatchId)))
            .ToList();

        return new PagedResult<StockBalanceResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    /// <summary>
    /// Product names and reorder levels live in Products and on-hand quantities here, with no
    /// SQL join across the two modules — so which side drives the paging depends on the query.
    /// Unfiltered, Products pages (and searches, and name-orders) in SQL and only that page's
    /// balances are loaded. A needs-attention filter can't be paged that way — whether a
    /// product is low or expiring is only known once its balances and batch expiries are
    /// rolled up — so it rolls up every active product from lean, unpaged projections and
    /// pages the result in memory. That's bounded by the size of one hospital's catalog (a few
    /// thousand narrow rows), not by transaction history.
    /// </summary>
    public async Task<PagedResult<StockSummaryResponse>> GetSummaryPagedAsync(StockSummaryListQuery query, CancellationToken cancellationToken)
    {
        if (query.Filter is { } filter)
        {
            var allProducts = await _productService.GetStockProfilesAsync(query.Search, cancellationToken);
            var matching = (await BuildSummariesAsync(allProducts, productIds: null, query.StoreId, cancellationToken))
                .Where(s => StockSummaryCalculator.Matches(s, filter))
                .ToList();

            var page = matching.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
            return new PagedResult<StockSummaryResponse>(page, query.Page, query.PageSize, matching.Count);
        }

        var productPage = await _productService.GetPagedAsync(
            new ProductListQuery { Page = query.Page, PageSize = query.PageSize, Search = query.Search, IsActive = true },
            cancellationToken);

        var profiles = productPage.Items
            .Select(p => new ProductStockProfileResponse
            {
                Id = p.Id,
                Sku = p.Sku,
                ProductName = p.ProductName,
                GenericName = p.GenericName,
                ReorderLevel = p.ReorderLevel,
            })
            .ToList();

        var summaries = await BuildSummariesAsync(profiles, profiles.Select(p => p.Id).ToList(), query.StoreId, cancellationToken);
        return new PagedResult<StockSummaryResponse>(summaries, query.Page, query.PageSize, productPage.TotalCount);
    }

    public async Task<IReadOnlyList<StockSummaryResponse>> GetSummariesAsync(StockSummaryFilter filter, Guid? storeId, CancellationToken cancellationToken)
    {
        var allProducts = await _productService.GetStockProfilesAsync(search: null, cancellationToken);
        return (await BuildSummariesAsync(allProducts, productIds: null, storeId, cancellationToken))
            .Where(s => StockSummaryCalculator.Matches(s, filter))
            .ToList();
    }

    public async Task<IReadOnlyList<AvailableBatchResponse>> GetAvailableBatchesAsync(Guid productId, Guid? storeId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        var product = (await _productService.GetByIdAsync(productId, cancellationToken)).Value;
        if (store is null || product is null)
        {
            return [];
        }

        var inStock = (await _balanceRepository.GetLinesAsync([productId], store.Id, cancellationToken))
            .Where(l => l.QuantityOnHand > 0)
            .ToList();

        var batches = await _productBatchService.GetLookupAsync(inStock.Select(l => l.ProductBatchId), cancellationToken);
        var today = PharmacyClock.Today();
        var expiringSoonCutoff = today.AddDays(await ExpiryWindowDaysAsync(cancellationToken));

        return inStock
            .Select(line => (Line: line, Batch: batches.GetValueOrDefault(line.ProductBatchId)))
            .Where(x => x.Batch is { IsActive: true } && x.Batch.ExpiryDate >= today)
            .OrderBy(x => x.Batch!.ExpiryDate)
            .ThenBy(x => x.Batch!.BatchNo)
            .Select(x => new AvailableBatchResponse
            {
                ProductBatchId = x.Line.ProductBatchId,
                BatchNo = x.Batch!.BatchNo,
                ExpiryDate = x.Batch.ExpiryDate,
                QuantityOnHand = x.Line.QuantityOnHand,
                IsExpiringSoon = x.Batch.ExpiryDate <= expiringSoonCutoff,
                Mrp = x.Batch.Mrp,
                UnitPrice = PharmacyPricing.UnitPrice(product, x.Batch),
            })
            .ToList();
    }

    /// <summary>How many days ahead counts as "expiring soon": the pharmacy's own setting,
    /// or 30 days until one is saved.</summary>
    private async Task<int> ExpiryWindowDaysAsync(CancellationToken cancellationToken)
        => (await _settingsRepository.GetAsync(cancellationToken))?.ExpiryAlertDays ?? PharmacyClock.ExpiringSoonWindowDays;

    private async Task<List<StockSummaryResponse>> BuildSummariesAsync(
        IReadOnlyList<ProductStockProfileResponse> products,
        IReadOnlyCollection<Guid>? productIds,
        Guid? storeId,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return [];
        }

        var lines = await _balanceRepository.GetLinesAsync(productIds, storeId, cancellationToken);
        var batches = await _productBatchService.GetLookupAsync(
            lines.Where(l => l.QuantityOnHand > 0).Select(l => l.ProductBatchId), cancellationToken);

        var linesByProduct = lines.ToLookup(l => l.ProductId);
        var today = PharmacyClock.Today();
        var window = await ExpiryWindowDaysAsync(cancellationToken);

        return products
            .Select(p => StockSummaryCalculator.Build(p, linesByProduct[p.Id].ToList(), batches, today, window))
            .ToList();
    }
}
