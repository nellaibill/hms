using System.Text.Json;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

public interface ICounterService
{
    /// <summary>Items matching what was typed or scanned, each with its sellable stock and
    /// prices in one store. Items the store has in stock come first.</summary>
    Task<IReadOnlyList<CounterItemResponse>> SearchItemsAsync(string? search, Guid? storeId, int limit, CancellationToken cancellationToken);

    /// <summary>One item by product id — for refreshing a line (a recalled bill, a
    /// prescription) against stock as it is now.</summary>
    Task<IReadOnlyList<CounterItemResponse>> GetItemsAsync(IReadOnlyCollection<Guid> productIds, Guid? storeId, CancellationToken cancellationToken);

    /// <summary>Other products with the same generic that the store has in stock.</summary>
    Task<IReadOnlyList<CounterItemResponse>> GetSubstitutesAsync(Guid productId, Guid? storeId, CancellationToken cancellationToken);

    Task<Result<HeldBillResponse>> HoldAsync(HoldBillRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HeldBillResponse>> GetHeldAsync(Guid? storeId, CancellationToken cancellationToken);

    Task<Result<bool>> DiscardHeldAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>What the billing counter needs besides the bill itself: finding items fast, and
/// parking a bill.</summary>
internal class CounterService : ICounterService
{
    private const int MaxResults = 50;
    private const int MaxSubstituteCandidates = 40;

    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacySaleRepository _saleRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly ISellableStockReader _stockReader;
    private readonly IProductService _productService;

    public CounterService(
        IPharmacyStoreRepository storeRepository,
        IPharmacySaleRepository saleRepository,
        IPharmacyUnitOfWork unitOfWork,
        ISellableStockReader stockReader,
        IProductService productService)
    {
        _storeRepository = storeRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
        _stockReader = stockReader;
        _productService = productService;
    }

    public async Task<IReadOnlyList<CounterItemResponse>> SearchItemsAsync(string? search, Guid? storeId, int limit, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        if (store is null || string.IsNullOrWhiteSpace(search))
        {
            return [];
        }

        var page = await _productService.GetPagedAsync(
            new ProductListQuery { Search = search.Trim(), IsActive = true, Page = 1, PageSize = Math.Clamp(limit, 1, MaxResults), Sort = "productName" },
            cancellationToken);

        return await BuildAsync(page.Items, store.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CounterItemResponse>> GetItemsAsync(IReadOnlyCollection<Guid> productIds, Guid? storeId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        if (store is null || productIds.Count == 0)
        {
            return [];
        }

        var products = await _productService.GetByIdsAsync(productIds.Distinct().Take(200).ToList(), cancellationToken);
        return await BuildAsync(products, store.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CounterItemResponse>> GetSubstitutesAsync(Guid productId, Guid? storeId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        var product = (await _productService.GetByIdAsync(productId, cancellationToken)).Value;
        if (store is null || product is null || string.IsNullOrWhiteSpace(product.GenericName))
        {
            return [];
        }

        // Same generic, exactly — "Paracetamol 650 mg" is not a substitute for "Paracetamol
        // 500 mg". The search narrows the catalog; the equality test decides.
        var generic = product.GenericName.Trim();
        var candidates = (await _productService.GetStockProfilesAsync(generic, cancellationToken))
            .Where(p => p.Id != productId && string.Equals(p.GenericName?.Trim(), generic, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSubstituteCandidates)
            .Select(p => p.Id)
            .ToList();

        if (candidates.Count == 0)
        {
            return [];
        }

        var products = await _productService.GetByIdsAsync(candidates, cancellationToken);
        var items = await BuildAsync(products, store.Id, cancellationToken);
        return items.Where(i => i.AvailableQuantity > 0).ToList();
    }

    public async Task<Result<HeldBillResponse>> HoldAsync(HoldBillRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<HeldBillResponse>.Failure(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        try
        {
            using var _ = JsonDocument.Parse(request.Payload);
        }
        catch (JsonException)
        {
            return Result<HeldBillResponse>.Failure(PharmacyErrorCodes.InvalidState, "The held bill could not be read.");
        }

        var held = PharmacyHeldBill.Create(store.Id, request.Label, request.ItemCount, request.Amount, request.Payload, actorId);
        await _saleRepository.AddHeldAsync(held, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HeldBillResponse>.Success(ToResponse(held));
    }

    public async Task<IReadOnlyList<HeldBillResponse>> GetHeldAsync(Guid? storeId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(storeId, cancellationToken);
        if (store is null)
        {
            return [];
        }

        return (await _saleRepository.GetHeldForStoreAsync(store.Id, cancellationToken)).Select(ToResponse).ToList();
    }

    public async Task<Result<bool>> DiscardHeldAsync(Guid id, CancellationToken cancellationToken)
    {
        var held = await _saleRepository.GetHeldAsync(id, cancellationToken);
        if (held is null)
        {
            return Result<bool>.Failure(PharmacyErrorCodes.NotFound, "The held bill was not found.");
        }

        _saleRepository.RemoveHeld(held);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private async Task<IReadOnlyList<CounterItemResponse>> BuildAsync(IReadOnlyCollection<ProductResponse> products, Guid storeId, CancellationToken cancellationToken)
    {
        var stock = await _stockReader.ReadAsync(products, storeId, cancellationToken);

        return products
            .Select(product => ToItem(stock[product.Id]))
            .OrderByDescending(item => item.AvailableQuantity > 0)
            .ThenBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static CounterItemResponse ToItem(SellableItem item) => new()
    {
        ProductId = item.Product.Id,
        ProductName = item.Product.ProductName,
        GenericName = item.Product.GenericName,
        Strength = item.Product.Strength,
        DosageForm = item.Product.DosageForm,
        Schedule = item.Product.Schedule,
        RackLocation = item.Product.RackLocation,
        HsnCode = item.Product.HsnCode,
        UnitsPerPack = item.Product.UnitsPerPack > 0 ? item.Product.UnitsPerPack : 1m,
        GstRatePercent = item.Product.GstRatePercent ?? 0m,
        AvailableQuantity = item.AvailableQuantity,
        UnitPrice = item.Batches.Count > 0
            ? item.Batches[0].UnitPrice
            : Math.Round((item.Product.SellingPrice > 0 ? item.Product.SellingPrice : item.Product.Mrp) / (item.Product.UnitsPerPack > 0 ? item.Product.UnitsPerPack : 1m), 4, MidpointRounding.AwayFromZero),
        Batches = item.Batches
            .Select(b => new CounterBatchResponse
            {
                ProductBatchId = b.Batch.Id,
                BatchNo = b.Batch.BatchNo,
                ExpiryDate = b.Batch.ExpiryDate,
                QuantityOnHand = b.Quantity,
                UnitPrice = b.UnitPrice,
                UnitMrp = b.UnitMrp,
            })
            .ToList(),
    };

    private static HeldBillResponse ToResponse(PharmacyHeldBill held) => new()
    {
        Id = held.Id,
        StoreId = held.StoreId,
        Label = held.Label,
        ItemCount = held.ItemCount,
        Amount = held.Amount,
        Payload = held.Payload,
        CreatedAt = held.CreatedAt,
    };
}
