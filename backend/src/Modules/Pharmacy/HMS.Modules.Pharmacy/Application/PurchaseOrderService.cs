using HMS.Modules.Masters.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

public interface IPurchaseOrderService
{
    Task<Result<PurchaseOrderResponse>> CreateAsync(SavePurchaseOrderRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Replaces a draft order's supplier, dates and lines.</summary>
    Task<Result<PurchaseOrderResponse>> UpdateAsync(Guid id, SavePurchaseOrderRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PurchaseOrderResponse>> PlaceAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PurchaseOrderResponse>> CancelAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PurchaseOrderResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PurchaseOrderResponse>> GetPagedAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken);

    /// <summary>Products at or below their reorder level, with a suggested order quantity.</summary>
    /// <param name="includeCost">False withholds the last purchase rate from a caller who may not see cost.</param>
    Task<IReadOnlyList<ReorderSuggestionResponse>> GetReorderSuggestionsAsync(Guid? storeId, bool includeCost, CancellationToken cancellationToken);
}

/// <summary>
/// Purchase orders, and the list that feeds them: what has run low. The suggestion is
/// deliberately simple arithmetic a pharmacist can check by eye — fill up to the product's
/// maximum level (or twice its reorder level where no maximum is set), less what is already
/// on order, rounded up to whole packs.
/// </summary>
internal class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPharmacyProcurementRepository _repository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IStockBalanceService _stockBalanceService;
    private readonly IProductService _productService;
    private readonly ISupplierService _supplierService;

    public PurchaseOrderService(
        IPharmacyProcurementRepository repository,
        IPharmacyStoreRepository storeRepository,
        IPharmacyUnitOfWork unitOfWork,
        IStockBalanceService stockBalanceService,
        IProductService productService,
        ISupplierService supplierService)
    {
        _repository = repository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _stockBalanceService = stockBalanceService;
        _productService = productService;
        _supplierService = supplierService;
    }

    public async Task<Result<PurchaseOrderResponse>> CreateAsync(SavePurchaseOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Fail(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var prepared = await PrepareAsync(request, cancellationToken);
        if (!prepared.IsSuccess)
        {
            return Fail(prepared.ErrorCode!, prepared.Error!);
        }

        var (supplierName, lines) = prepared.Value;
        var orderNo = await _unitOfWork.NextNumberAsync(DocumentSeries.PurchaseOrder, cancellationToken);
        var order = PharmacyPurchaseOrder.Create(orderNo, store.Id, request.SupplierId, supplierName, PharmacyClock.Today(), request.ExpectedDate, request.Remarks, actorId);

        foreach (var line in lines)
        {
            order.AddLine(line.Product.Id, line.Product.ProductName, line.Quantity, line.Rate, line.Product.GstRatePercent ?? 0m);
        }

        if (request.PlaceOrder)
        {
            order.MarkOrdered(actorId);
        }

        await _repository.AddOrderAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PurchaseOrderResponse>.Success(ToResponse(order, store.Name, order.Lines.Count, order.EstimatedAmount, includeLines: true));
    }

    public async Task<Result<PurchaseOrderResponse>> UpdateAsync(Guid id, SavePurchaseOrderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetOrderAsync(id, cancellationToken);
        if (order is null)
        {
            return Fail(PharmacyErrorCodes.NotFound, "The purchase order was not found.");
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return Fail(PharmacyErrorCodes.InvalidState, $"Order {order.OrderNo} has been sent and can no longer be edited.");
        }

        var prepared = await PrepareAsync(request, cancellationToken);
        if (!prepared.IsSuccess)
        {
            return Fail(prepared.ErrorCode!, prepared.Error!);
        }

        var (supplierName, lines) = prepared.Value;
        order.Revise(request.SupplierId, supplierName, request.ExpectedDate, request.Remarks, actorId);
        foreach (var line in lines)
        {
            order.AddLine(line.Product.Id, line.Product.ProductName, line.Quantity, line.Rate, line.Product.GstRatePercent ?? 0m);
        }

        if (request.PlaceOrder)
        {
            order.MarkOrdered(actorId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await RespondAsync(order, cancellationToken);
    }

    public async Task<Result<PurchaseOrderResponse>> PlaceAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
        => await ChangeAsync(id, order => order.MarkOrdered(actorId), cancellationToken);

    public async Task<Result<PurchaseOrderResponse>> CancelAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
        => await ChangeAsync(id, order => order.Cancel(actorId), cancellationToken);

    public async Task<Result<PurchaseOrderResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _repository.GetOrderAsync(id, cancellationToken);
        return order is null ? Fail(PharmacyErrorCodes.NotFound, "The purchase order was not found.") : await RespondAsync(order, cancellationToken);
    }

    public async Task<PagedResult<PurchaseOrderResponse>> GetPagedAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetOrdersPagedAsync(query, cancellationToken);
        var storeNames = (await _storeRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);

        var responses = items
            .Select(i => ToResponse(i.Order, storeNames.GetValueOrDefault(i.Order.StoreId) ?? string.Empty, i.LineCount, i.Amount, includeLines: false))
            .ToList();

        return new PagedResult<PurchaseOrderResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<ReorderSuggestionResponse>> GetReorderSuggestionsAsync(Guid? storeId, bool includeCost, CancellationToken cancellationToken)
    {
        var low = await _stockBalanceService.GetSummariesAsync(StockSummaryFilter.NeedsReorder, storeId, cancellationToken);
        if (low.Count == 0)
        {
            return [];
        }

        var productIds = low.Select(s => s.ProductId).ToList();
        var products = (await _productService.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(p => p.Id);
        var onOrder = await _repository.GetOnOrderQuantitiesAsync(storeId, cancellationToken);
        var lastPurchases = await _repository.GetLastPurchasesAsync(productIds, cancellationToken);

        return low
            .Where(summary => products.ContainsKey(summary.ProductId))
            .Select(summary =>
            {
                var product = products[summary.ProductId];
                var unitsPerPack = product.UnitsPerPack > 0 ? product.UnitsPerPack : 1m;
                var onOrderPacks = onOrder.GetValueOrDefault(product.Id);
                var last = lastPurchases.GetValueOrDefault(product.Id);

                return new ReorderSuggestionResponse
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    GenericName = product.GenericName,
                    UnitsPerPack = unitsPerPack,
                    AvailableQuantity = summary.AvailableQuantity,
                    ReorderLevel = product.ReorderLevel,
                    Status = summary.Status,
                    OnOrderQuantity = onOrderPacks,
                    SuggestedQuantity = SuggestPacks(summary.AvailableQuantity, product.ReorderLevel, product.MaxStockLevel, unitsPerPack, onOrderPacks),
                    LastSupplierId = last?.SupplierId,
                    LastSupplierName = last?.SupplierName,
                    LastPurchaseRate = includeCost ? last?.PurchaseRate ?? (product.CostPrice > 0 ? product.CostPrice : null) : null,
                    GstRatePercent = product.GstRatePercent ?? 0m,
                };
            })
            .OrderBy(s => s.LastSupplierName ?? "￿", StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Packs to order: enough to bring stock up to the maximum level — or to twice the
    /// reorder level where no maximum is set — less what is already on order, in whole packs.
    /// Never negative; zero when open orders already cover it.
    /// </summary>
    internal static decimal SuggestPacks(decimal availableUnits, decimal reorderLevel, decimal maxStockLevel, decimal unitsPerPack, decimal onOrderPacks)
    {
        var target = maxStockLevel > reorderLevel ? maxStockLevel : reorderLevel * 2m;
        var neededUnits = target - availableUnits - (onOrderPacks * unitsPerPack);
        return neededUnits <= 0 ? 0m : Math.Ceiling(neededUnits / unitsPerPack);
    }

    private sealed record PreparedLine(ProductResponse Product, decimal Quantity, decimal Rate);

    private async Task<Result<(string SupplierName, IReadOnlyList<PreparedLine> Lines)>> PrepareAsync(SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var supplierResult = await _supplierService.GetByIdAsync(request.SupplierId, cancellationToken);
        if (!supplierResult.IsSuccess || !supplierResult.Value!.IsActive)
        {
            return Result<(string, IReadOnlyList<PreparedLine>)>.Failure(PharmacyErrorCodes.InvalidSupplier, "The supplier was not found or is inactive.");
        }

        var products = await _productService.GetLookupAsync(request.Lines.Select(l => l.ProductId), cancellationToken);
        var lines = new List<PreparedLine>(request.Lines.Count);

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
            {
                return Result<(string, IReadOnlyList<PreparedLine>)>.Failure(PharmacyErrorCodes.InvalidProduct, $"Line {i + 1}: the product was not found or is inactive.");
            }

            lines.Add(new PreparedLine(product, line.Quantity, line.ExpectedRate ?? product.CostPrice));
        }

        return Result<(string, IReadOnlyList<PreparedLine>)>.Success((supplierResult.Value.SupplierName, lines));
    }

    private async Task<Result<PurchaseOrderResponse>> ChangeAsync(Guid id, Action<PharmacyPurchaseOrder> change, CancellationToken cancellationToken)
    {
        var order = await _repository.GetOrderAsync(id, cancellationToken);
        if (order is null)
        {
            return Fail(PharmacyErrorCodes.NotFound, "The purchase order was not found.");
        }

        try
        {
            change(order);
        }
        catch (InvalidOperationException ex)
        {
            return Fail(PharmacyErrorCodes.InvalidState, ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await RespondAsync(order, cancellationToken);
    }

    private async Task<Result<PurchaseOrderResponse>> RespondAsync(PharmacyPurchaseOrder order, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(order.StoreId, cancellationToken);
        return Result<PurchaseOrderResponse>.Success(ToResponse(order, store?.Name ?? string.Empty, order.Lines.Count, order.EstimatedAmount, includeLines: true));
    }

    private static Result<PurchaseOrderResponse> Fail(string code, string message) => Result<PurchaseOrderResponse>.Failure(code, message);

    private static PurchaseOrderResponse ToResponse(PharmacyPurchaseOrder order, string storeName, int lineCount, decimal amount, bool includeLines) => new()
    {
        Id = order.Id,
        OrderNo = order.OrderNo,
        StoreId = order.StoreId,
        StoreName = storeName,
        SupplierId = order.SupplierId,
        SupplierName = order.SupplierName,
        OrderDate = order.OrderDate,
        ExpectedDate = order.ExpectedDate,
        Status = order.Status,
        EstimatedAmount = GstMath.Round2(amount),
        Remarks = order.Remarks,
        LineCount = lineCount,
        CreatedAt = order.CreatedAt,
        Lines = includeLines
            ? order.Lines.Select(l => new PurchaseOrderLineResponse
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                ReceivedQuantity = l.ReceivedQuantity,
                PendingQuantity = l.PendingQuantity,
                ExpectedRate = l.ExpectedRate,
                GstRatePercent = l.GstRatePercent,
            }).ToList()
            : [],
    };
}
