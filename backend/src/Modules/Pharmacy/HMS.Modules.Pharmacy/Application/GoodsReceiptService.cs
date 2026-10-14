using HMS.Modules.Masters.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

/// <summary>Public for the same reason as IStockReceiptService: its controller takes it as a
/// constructor dependency.</summary>
public interface IGoodsReceiptService
{
    Task<Result<GoodsReceiptResponse>> CreateAsync(CreateGoodsReceiptRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<GoodsReceiptResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<GoodsReceiptResponse>> GetPagedAsync(GoodsReceiptListQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Books a supplier's invoice: brings each line's batch into the catalog (creating it, or
/// updating its expiry and prices), puts the goods into the store, and keeps the invoice as a
/// document the stock ledger points back to.
/// </summary>
internal class GoodsReceiptService : IGoodsReceiptService
{
    private readonly IPharmacyGoodsReceiptRepository _repository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IStockPoster _stockPoster;
    private readonly IProductService _productService;
    private readonly IProductBatchService _productBatchService;
    private readonly ISupplierService _supplierService;

    public GoodsReceiptService(
        IPharmacyGoodsReceiptRepository repository,
        IPharmacyStoreRepository storeRepository,
        IPharmacyUnitOfWork unitOfWork,
        IStockPoster stockPoster,
        IProductService productService,
        IProductBatchService productBatchService,
        ISupplierService supplierService)
    {
        _repository = repository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _stockPoster = stockPoster;
        _productService = productService;
        _productBatchService = productBatchService;
        _supplierService = supplierService;
    }

    public async Task<Result<GoodsReceiptResponse>> CreateAsync(CreateGoodsReceiptRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Failure(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var supplierResult = await _supplierService.GetByIdAsync(request.SupplierId, cancellationToken);
        if (!supplierResult.IsSuccess)
        {
            return Failure(PharmacyErrorCodes.InvalidSupplier, "The supplier was not found.");
        }

        var supplier = supplierResult.Value!;
        if (!supplier.IsActive)
        {
            return Failure(PharmacyErrorCodes.InvalidSupplier, $"Supplier '{supplier.SupplierName}' is inactive.");
        }

        // The same invoice keyed twice would double the stock — the commonest purchase-entry
        // mistake there is.
        if (await _repository.ExistsForSupplierInvoiceAsync(supplier.Id, request.SupplierInvoiceNo.Trim(), cancellationToken))
        {
            return Failure(
                PharmacyErrorCodes.DuplicateSupplierInvoice,
                $"Invoice '{request.SupplierInvoiceNo.Trim()}' from {supplier.SupplierName} has already been entered.");
        }

        var products = await _productService.GetLookupAsync(request.Lines.Select(l => l.ProductId), cancellationToken);
        var today = PharmacyClock.Today();

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                return Failure(PharmacyErrorCodes.InvalidProduct, $"Line {i + 1}: the product was not found.");
            }

            if (!product.IsActive)
            {
                return Failure(PharmacyErrorCodes.ProductInactive, $"Line {i + 1}: {product.ProductName} is inactive.");
            }

            if (line.ExpiryDate < today)
            {
                return Failure(PharmacyErrorCodes.BatchExpired, $"Line {i + 1}: batch {line.BatchNo.Trim()} of {product.ProductName} has already expired.");
            }
        }

        // Batches live in the Products module, so they are saved before the receipt and not
        // in its transaction. That order is the safe one: a batch with no stock behind it is
        // harmless, stock pointing at a batch that failed to save would not be.
        var batches = new List<ProductBatchResponse>(request.Lines.Count);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            var batchResult = await _productBatchService.UpsertAsync(
                line.ProductId,
                new UpsertProductBatchRequest
                {
                    BatchNo = line.BatchNo,
                    ManufactureDate = line.ManufactureDate,
                    ExpiryDate = line.ExpiryDate,
                    Mrp = line.Mrp,
                    PurchaseRate = line.PurchaseRate,
                    SellingRate = line.SellingRate,
                },
                actorId,
                cancellationToken);

            if (!batchResult.IsSuccess)
            {
                return Failure(PharmacyErrorCodes.InvalidBatch, $"Line {i + 1}: {batchResult.Error}");
            }

            batches.Add(batchResult.Value!);
        }

        var receiptNo = await _unitOfWork.NextNumberAsync(DocumentSeries.GoodsReceipt, cancellationToken);

        var result = await _unitOfWork.ExecuteAsync(
            async () =>
            {
                var receipt = PharmacyGoodsReceipt.Create(
                    receiptNo, store.Id, supplier.Id, supplier.SupplierName, request.SupplierInvoiceNo, request.SupplierInvoiceDate,
                    request.ReceivedDate ?? today, request.IsInterState, request.PurchaseOrderId, request.OtherCharges, request.Remarks, actorId);

                for (var i = 0; i < request.Lines.Count; i++)
                {
                    var line = request.Lines[i];
                    var product = products[line.ProductId];
                    var batch = batches[i];
                    var gstRate = line.GstRatePercent ?? product.GstRatePercent ?? 0m;
                    var amount = GstMath.Exclusive(line.Quantity * line.PurchaseRate, line.DiscountPercent, gstRate);

                    var receiptLine = receipt.AddLine(
                        product.Id, product.ProductName, product.HsnCode, batch.Id, batch.BatchNo, batch.ExpiryDate,
                        product.UnitsPerPack > 0 ? product.UnitsPerPack : 1m, line.Quantity, line.FreeQuantity, line.PurchaseRate, line.Mrp,
                        line.SellingRate, line.DiscountPercent, gstRate, amount.Gross, amount.Discount, amount.Taxable, amount.Tax);

                    var posted = await _stockPoster.PostAsync(
                        new StockMovement(
                            TransactionType.Receipt, store.Id, product.Id, batch.Id, receiptLine.ReceivedUnits,
                            UnitCost: receiptLine.UnitCost,
                            SourceType: StockSourceType.GoodsReceipt, SourceId: receipt.Id, SourceNo: receipt.ReceiptNo,
                            Remarks: $"{supplier.SupplierName} · Inv {receipt.SupplierInvoiceNo}"),
                        actorId,
                        cancellationToken);

                    if (!posted.IsSuccess)
                    {
                        return Result<PharmacyGoodsReceipt>.Failure(posted.ErrorCode!, posted.Error!);
                    }
                }

                receipt.Close();
                await _repository.AddAsync(receipt, cancellationToken);
                return Result<PharmacyGoodsReceipt>.Success(receipt);
            },
            cancellationToken);

        return result.IsSuccess
            ? Result<GoodsReceiptResponse>.Success(ToResponse(result.Value!, store.Name, result.Value!.Lines.Count, includeLines: true))
            : Failure(result.ErrorCode!, result.Error!);
    }

    public async Task<Result<GoodsReceiptResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var receipt = await _repository.GetByIdAsync(id, cancellationToken);
        if (receipt is null)
        {
            return Failure(PharmacyErrorCodes.NotFound, "The goods receipt was not found.");
        }

        var store = await _storeRepository.GetByIdAsync(receipt.StoreId, cancellationToken);
        return Result<GoodsReceiptResponse>.Success(ToResponse(receipt, store?.Name ?? string.Empty, receipt.Lines.Count, includeLines: true));
    }

    public async Task<PagedResult<GoodsReceiptResponse>> GetPagedAsync(GoodsReceiptListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var storeNames = (await _storeRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);

        var responses = items
            .Select(i => ToResponse(i.Receipt, storeNames.GetValueOrDefault(i.Receipt.StoreId) ?? string.Empty, i.LineCount, includeLines: false))
            .ToList();

        return new PagedResult<GoodsReceiptResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    private static Result<GoodsReceiptResponse> Failure(string errorCode, string message) => Result<GoodsReceiptResponse>.Failure(errorCode, message);

    private static GoodsReceiptResponse ToResponse(PharmacyGoodsReceipt receipt, string storeName, int lineCount, bool includeLines) => new()
    {
        Id = receipt.Id,
        ReceiptNo = receipt.ReceiptNo,
        StoreId = receipt.StoreId,
        StoreName = storeName,
        SupplierId = receipt.SupplierId,
        SupplierName = receipt.SupplierName,
        SupplierInvoiceNo = receipt.SupplierInvoiceNo,
        SupplierInvoiceDate = receipt.SupplierInvoiceDate,
        ReceivedDate = receipt.ReceivedDate,
        IsInterState = receipt.IsInterState,
        PurchaseOrderId = receipt.PurchaseOrderId,
        GrossAmount = receipt.GrossAmount,
        DiscountAmount = receipt.DiscountAmount,
        TaxableAmount = receipt.TaxableAmount,
        TaxAmount = receipt.TaxAmount,
        OtherCharges = receipt.OtherCharges,
        RoundOff = receipt.RoundOff,
        NetAmount = receipt.NetAmount,
        Remarks = receipt.Remarks,
        LineCount = lineCount,
        CreatedAt = receipt.CreatedAt,
        Lines = includeLines ? receipt.Lines.Select(ToLineResponse).ToList() : [],
    };

    internal static GoodsReceiptLineResponse ToLineResponse(PharmacyGoodsReceiptLine line) => new()
    {
        Id = line.Id,
        LineNo = line.LineNo,
        ProductId = line.ProductId,
        ProductName = line.ProductName,
        HsnCode = line.HsnCode,
        ProductBatchId = line.ProductBatchId,
        BatchNo = line.BatchNo,
        ExpiryDate = line.ExpiryDate,
        UnitsPerPack = line.UnitsPerPack,
        Quantity = line.Quantity,
        FreeQuantity = line.FreeQuantity,
        PurchaseRate = line.PurchaseRate,
        Mrp = line.Mrp,
        SellingRate = line.SellingRate,
        DiscountPercent = line.DiscountPercent,
        GstRatePercent = line.GstRatePercent,
        TaxableAmount = line.TaxableAmount,
        TaxAmount = line.TaxAmount,
        LineTotal = line.LineTotal,
        ReceivedUnits = line.ReceivedUnits,
        ReturnedUnits = line.ReturnedUnits,
    };
}
